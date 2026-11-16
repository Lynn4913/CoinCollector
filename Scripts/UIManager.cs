using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CoinCollector
{
    /// <summary>
    /// 游戏内界面管理：分数 HUD、计时结果、胜利面板与失败面板。
    /// 所有 Inspector 引用由 ProjectBuilder / SceneUpgrader 自动连接。
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private Text scoreText;
        [SerializeField] private string scoreFormat = "Coins: {0} / {1}";

        [Header("Victory Panel")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Text victoryScoreText;
        [SerializeField] private Text victoryTimeText;
        [SerializeField] private Text victoryBestTimeText;
        [SerializeField] private GameObject victoryNewRecordLabel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Game Over Panel")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text gameOverScoreText;
        [SerializeField] private Text gameOverTimeText;
        [SerializeField] private Button gameOverRestartButton;
        [SerializeField] private Button gameOverMainMenuButton;

        [Header("Text Formats")]
        [SerializeField] private string victoryScoreFormat = "Coins: {0} / {1}";
        [SerializeField] private string completionTimeFormat = "Completion Time: {0}";
        [SerializeField] private string bestTimeFormat = "Best Time: {0}";
        [SerializeField] private string gameOverScoreFormat = "Coins: {0} / {1}";
        [SerializeField] private string gameOverTimeFormat = "Time: {0}";

        [Header("Scene Names")]
        [SerializeField] private string gameSceneName = "MorandiGameScene";
        [SerializeField] private string mainMenuSceneName = "MorandiMainMenu";

        private void Awake()
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(false);
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        /// <summary>更新 HUD 分数文本。</summary>
        public void UpdateScore(int score, int targetScore)
        {
            if (scoreText == null)
            {
                Debug.LogError("[UIManager] scoreText 未连接，无法显示分数。", this);
                return;
            }

            scoreText.text = string.Format(scoreFormat, score, targetScore);
        }

        /// <summary>
        /// 显示胜利面板：最终分数、本次用时、最高纪录，并在破纪录时显示 New Record!。
        /// </summary>
        public void ShowVictory(int score, int targetScore, float completionSeconds,
            float bestSeconds, bool isNewRecord)
        {
            if (victoryPanel == null)
            {
                Debug.LogError("[UIManager] victoryPanel 未连接，无法显示胜利面板。", this);
                return;
            }

            // 胜利与失败互斥：显示胜利面板前先隐藏失败面板
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            if (victoryScoreText != null)
            {
                victoryScoreText.text = string.Format(victoryScoreFormat, score, targetScore);
            }

            if (victoryTimeText != null)
            {
                victoryTimeText.text = string.Format(
                    completionTimeFormat, GameManager.FormatTime(completionSeconds));
            }

            if (victoryBestTimeText != null)
            {
                string bestText = bestSeconds < 0f
                    ? GameManager.NoRecordText
                    : GameManager.FormatTime(bestSeconds);
                victoryBestTimeText.text = string.Format(bestTimeFormat, bestText);
            }

            if (victoryNewRecordLabel != null)
            {
                victoryNewRecordLabel.SetActive(isNewRecord);
            }

            victoryPanel.SetActive(true);
        }

        /// <summary>显示失败面板：当前分数与死亡时用时。</summary>
        public void ShowGameOver(int score, int targetScore, float survivedSeconds)
        {
            if (gameOverPanel == null)
            {
                Debug.LogError("[UIManager] gameOverPanel 未连接，无法显示失败面板。", this);
                return;
            }

            // 胜利与失败互斥：显示失败面板前先隐藏胜利面板
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(false);
            }

            if (gameOverScoreText != null)
            {
                gameOverScoreText.text = string.Format(gameOverScoreFormat, score, targetScore);
            }

            if (gameOverTimeText != null)
            {
                gameOverTimeText.text = string.Format(
                    gameOverTimeFormat, GameManager.FormatTime(survivedSeconds));
            }

            if (victoryNewRecordLabel != null)
            {
                victoryNewRecordLabel.SetActive(false);
            }

            gameOverPanel.SetActive(true);
        }

        /// <summary>重新开始游戏：重新加载关卡场景，分数与计时随之重置。</summary>
        public void RestartGame()
        {
            LoadSceneOrReport(gameSceneName);
        }

        /// <summary>返回主菜单。</summary>
        public void GoToMainMenu()
        {
            LoadSceneOrReport(mainMenuSceneName);
        }

        private void LoadSceneOrReport(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[UIManager] 场景名为空，无法切换场景。");
                return;
            }

            if (!IsSceneAvailable(sceneName))
            {
                Debug.LogError("[UIManager] 场景 \"" + sceneName + "\" 不在 Build Settings 中，无法加载。请检查 File → Build Settings。");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        private static bool IsSceneAvailable(string sceneName)
        {
            int count = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < count; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (string.Equals(name, sceneName, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
