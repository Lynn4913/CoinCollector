

using System.Collections;
using UnityEngine;
using TMPro;

namespace CoinCollector
{
    /// <summary>
    /// 一局游戏的三种状态：进行中 / 胜利 / 失败。
    /// Victory 与 GameOver 互斥，且只能从 Playing 切换一次。
    /// </summary>
    public enum GameState
    {
        Playing,
        Victory,
        GameOver
    }

    /// <summary>
    /// 游戏核心状态管理：分数、计时、最高纪录、胜利与失败判定。如果用布尔值，则会出现失败又成功的错误情况
    /// 公开成员向下兼容：Score / TargetScore / IsVictory / AddScore / PlayCoinPickupSound 均保留。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        /// <summary>最高纪录（最快通关秒数）在 PlayerPrefs 中的存储键。</summary>
        public const string BestTimePlayerPrefsKey = "CoinCollector.BestTimeSeconds";

        /// <summary>没有纪录时显示的时间占位文本。</summary>
        public const string NoRecordText = "--:--";

        private static GameManager instance;

        public static GameManager Instance
        {
            get { return instance; }
        }

        [Header("Score")]
        [Tooltip("达到该分数即胜利")]
        [SerializeField] private int targetScore = 10;

        [Header("References")]
        [SerializeField] private UIManager uiManager;
        [SerializeField] private PlayerMovement playerMovement;
        [Tooltip("椭圆追击者；为空时会在 Start 中自动查找场景里的 Chaser")]
        [SerializeField] private Chaser chaser;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip coinPickupClip;
        [SerializeField] private AudioClip victoryClip;
        [Tooltip("可选：失败音效。留空则死亡时静音（绝不会播放胜利音效）")]
        [SerializeField] private AudioClip gameOverClip;

        [Header("Timer")]
        [SerializeField] private TMP_Text timerText;
        [Tooltip("计时器显示格式，{0}=分钟，{1}=秒")]
        [SerializeField] private string timerFormat = "TIME {0:00}:{1:00}";

        [Header("Victory UI")]
        [Tooltip("胜利面板弹出前的延迟秒数。0 = 立即弹出（保持原行为）；特殊金币演出时会临时抬高该值")]
        [SerializeField] private float victoryUiDelaySeconds = 0f;

        private int score;
        private GameState state;
        private float elapsedTime;
        private bool isTimerRunning;
        private bool isPaused;
        private float pendingVictoryUiDelay;

        public int Score
        {
            get { return score; }
        }

        public int TargetScore
        {
            get { return targetScore; }
        }

        /// <summary>当前游戏状态（Playing / Victory / GameOver）。</summary>
        public GameState CurrentState
        {
            get { return state; }
        }

        /// <summary>是否已胜利（兼容旧调用）。</summary>
        public bool IsVictory
        {
            get { return state == GameState.Victory; }
        }

        /// <summary>是否已失败（被追击者接触）。</summary>
        public bool IsGameOver
        {
            get { return state == GameState.GameOver; }
        }

        /// <summary>是否仍在游戏中（唯一可以继续计分 / 计时的状态）。</summary>
        public bool IsPlaying
        {
            get { return state == GameState.Playing; }
        }

        public bool IsPaused
        {
            get { return isPaused; }
        }

        /// <summary>本局已用时（秒）。</summary>
        public float ElapsedTime
        {
            get { return elapsedTime; }
        }

        /// <summary>场景中的常驻追击者引用（供特殊金币系统取基准速度；可能为 null）。</summary>
        public Chaser Chaser
        {
            get { return chaser; }
        }

        /// <summary>
        /// 由特殊金币系统在加分前调用：若这枚金币触发胜利，
        /// 胜利面板将推迟到特殊演出结束后再弹出（取两者较大值，不叠加）。
        /// </summary>
        public void SetPendingVictoryUiDelay(float seconds)
        {
            pendingVictoryUiDelay = Mathf.Max(0f, seconds);
        }

        /// <summary>本局已用时的 MM:SS 文本。</summary>
        public string FormattedTime
        {
            get { return FormatTime(elapsedTime); }
        }

        /// <summary>历史最快通关秒数；小于 0 表示还没有纪录。</summary>
        public float BestTimeSeconds
        {
            get { return LoadBestTimeSeconds(); }
        }

        /// <summary>是否已有历史纪录。</summary>
        public bool HasBestRecord
        {
            get { return LoadBestTimeSeconds() >= 0f; }
        }

        /// <summary>历史最快通关的 MM:SS 文本；无纪录时为 --:--。</summary>
        public string FormattedBestTime
        {
            get
            {
                float best = LoadBestTimeSeconds();
                return best < 0f ? NoRecordText : FormatTime(best);
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning(
                    "[GameManager] 检测到重复实例，已销毁新实例。");
                Destroy(gameObject);
                return;
            }

            instance = this;
            score = 0;
            state = GameState.Playing;
            elapsedTime = 0f;
            // 计时在 Start() 中才启动，保证主菜单停留时间不计入游戏时间
            isTimerRunning = false;
            isPaused = false;
        }

        private void Start()
        {
            if (uiManager == null)
            {
                uiManager = Object.FindObjectOfType<UIManager>();
            }

            if (uiManager == null)
            {
                Debug.LogError("[GameManager] 未找到 UIManager。");
            }

            if (playerMovement == null)
            {
                playerMovement =
                    Object.FindObjectOfType<PlayerMovement>();
            }

            if (playerMovement == null)
            {
                Debug.LogError(
                    "[GameManager] 未找到 PlayerMovement。");
            }

            if (chaser == null)
            {
                chaser = Object.FindObjectOfType<Chaser>();
            }

            if (timerText == null)
            {
                Debug.LogWarning(
                    "[GameManager] 尚未绑定 Timer Text，计时仍会进行但不会显示。");
            }

            RefreshHud();
            RefreshTimer();
            isTimerRunning = true;
        }

        private void Update()
        {
            if (!isTimerRunning || isPaused)
            {
                return;
            }

            elapsedTime += Time.deltaTime;//每帧更新计时
            RefreshTimer();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>暂停 / 恢复。暂停时计时同步停止，游戏状态不变。</summary>
        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }

        public void PlayCoinPickupSound()
        {
            if (audioSource == null || coinPickupClip == null)
            {
                Debug.LogWarning(
                    "[GameManager] 金币收集音效尚未配置。", this);
                return;
            }

            audioSource.PlayOneShot(coinPickupClip);
        }

        /// <summary>
        /// 增加分数。只有 Playing 状态才会加分，胜利或死亡后一律忽略。
        /// </summary>
        public void AddScore(int amount)
        {
            if (!IsPlaying)
            {
                return;
            }

            if (amount <= 0)
            {
                Debug.LogWarning(
                    "[GameManager] 收到非正分数：" + amount);
                return;
            }

            score += amount;
            RefreshHud();//更新面板

            if (score >= targetScore)
            {
                TriggerVictory();
            }
        }

        /// <summary>
        /// 触发失败（由 Chaser 在真实物理接触时调用）。
        /// 只有 Playing 状态可切换，重复调用会被忽略。
        /// </summary>
        public void TriggerGameOver()
        {
            if (!IsPlaying)
            {
                return;
            }

            state = GameState.GameOver;
            isTimerRunning = false;//禁用计时和运动
            isPaused = false;

            // 死亡只播放失败音效，绝不播放胜利音效
            if (audioSource != null && gameOverClip != null)
            {
                audioSource.PlayOneShot(gameOverClip);
            }

            FreezePlayer();
            StopChaser();

            if (uiManager != null)
            {
                uiManager.ShowGameOver(score, targetScore, elapsedTime);
            }

            Debug.Log(
                "[GameManager] 游戏失败：被追击者接触。分数：" +
                score + " / " + targetScore +
                "，用时：" + FormattedTime +
                "（本次不更新最高纪录）");
        }

        private void TriggerVictory()
        {
            if (!IsPlaying)
            {
                return;
            }

            state = GameState.Victory;
            isTimerRunning = false;
            isPaused = false;

            // 只有胜利才参与最高纪录（最快通关时间）比较
            float previousBest = LoadBestTimeSeconds();
            bool isNewRecord = previousBest < 0f || elapsedTime < previousBest;
            if (isNewRecord)
            {
                SaveBestTimeSeconds(elapsedTime);
            }

            float bestTime = isNewRecord ? elapsedTime : previousBest;

            // 保留原来的胜利音效
            if (audioSource != null && victoryClip != null)
            {
                audioSource.PlayOneShot(victoryClip);
            }
            else
            {
                Debug.LogWarning(
                    "[GameManager] 胜利音效尚未配置。", this);
            }

            FreezePlayer();
            StopChaser();

            // 胜利面板延迟弹出：特殊金币演出先播完，避免两个 UI 重叠。
            // 默认延迟 0 → 行为与升级前完全一致
            float uiDelay = Mathf.Max(victoryUiDelaySeconds, pendingVictoryUiDelay);
            pendingVictoryUiDelay = 0f;

            if (uiDelay > 0f && uiManager != null)
            {
                StartCoroutine(ShowVictoryPanelDelayed(
                    uiDelay, score, targetScore, elapsedTime, bestTime, isNewRecord));
            }
            else
            {
                ShowVictoryPanel(score, targetScore, elapsedTime, bestTime, isNewRecord);
            }

            Debug.Log(
                "[GameManager] 胜利！最终分数：" +
                score + " / " + targetScore +
                "，用时：" + FormattedTime +
                "，最高纪录：" + FormatTime(bestTime) +
                (isNewRecord ? "（新纪录）" : "（未刷新纪录）") +
                (uiDelay > 0f ? "（胜利面板延迟 " + uiDelay.ToString("0.00") + " 秒弹出）" : ""));
        }

        /// <summary>延迟弹出胜利面板（协程）。延迟期间游戏已处于 Victory 状态：已停表、已冻结。</summary>
        private IEnumerator ShowVictoryPanelDelayed(
            float delay, int finalScore, int finalTarget, float completion,
            float bestTime, bool newRecord)
        {
            yield return new WaitForSeconds(delay);

            // 场景可能在延迟期间被重载（协程随宿主销毁则不会走到这里）
            if (uiManager != null && IsVictory)
            {
                ShowVictoryPanel(finalScore, finalTarget, completion, bestTime, newRecord);
            }
        }

        private void ShowVictoryPanel(
            int finalScore, int finalTarget, float completion,
            float bestTime, bool newRecord)
        {
            if (uiManager == null)
            {
                return;
            }

            uiManager.ShowVictory(finalScore, finalTarget, completion, bestTime, newRecord);
        }

        private void FreezePlayer()
        {
            if (playerMovement != null)
            {
                playerMovement.SetMovementEnabled(false);
            }
        }

        private void StopChaser()
        {
            if (chaser != null)
            {
                chaser.SetChasing(false);
            }
        }

        /// <summary>在 Inspector / 测试中手动绑定追击者。</summary>
        public void SetChaser(Chaser newChaser)
        {
            chaser = newChaser;
        }

        private void RefreshHud()
        {
            if (uiManager != null)
            {
                uiManager.UpdateScore(score, targetScore);
            }
        }

        private void RefreshTimer()
        {
            if (timerText == null)
            {
                return;
            }

            int totalSeconds = Mathf.FloorToInt(elapsedTime);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            timerText.text = string.Format(timerFormat, minutes, seconds);
        }

        /// <summary>把秒数格式化为 MM:SS。</summary>
        public static string FormatTime(float seconds)
        {
            if (seconds < 0f)
            {
                seconds = 0f;
            }

            int totalSeconds = Mathf.FloorToInt(seconds);
            int minutes = totalSeconds / 60;
            int rest = totalSeconds % 60;
            return string.Format("{0:00}:{1:00}", minutes, rest);
        }

        /// <summary>读取历史最快通关秒数；无纪录返回 -1。</summary>
        public static float LoadBestTimeSeconds()
        {
            if (!PlayerPrefs.HasKey(BestTimePlayerPrefsKey))
            {
                return -1f;
            }

            return PlayerPrefs.GetFloat(BestTimePlayerPrefsKey, -1f);
        }

        private static void SaveBestTimeSeconds(float seconds)
        {
            PlayerPrefs.SetFloat(BestTimePlayerPrefsKey, seconds);
            PlayerPrefs.Save();
        }

        /// <summary>清除最高纪录（用于测试）。</summary>
        public static void ClearBestRecord()
        {
            PlayerPrefs.DeleteKey(BestTimePlayerPrefsKey);
            PlayerPrefs.Save();
        }
    }
}
