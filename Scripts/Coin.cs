using UnityEngine;

namespace CoinCollector
{
    /// <summary>
    /// 单枚金币：旋转 + 轻微上下浮动，玩家进入触发器时被收集一次并销毁。
    /// 只有带 Player 标签的对象才能触发收集。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Coin : MonoBehaviour
    {
        [Header("Score")]
        [Tooltip("该金币的分值，可在 Inspector 中单独调整")]
        [SerializeField] private int scoreValue = 1;

        [Header("Special Coin")]
        [Tooltip("勾选后为特殊金币：分值不同 + 触发特殊演出。普通金币请保持关闭")]
        [SerializeField] private bool isSpecialCoin = false;
        [Tooltip("特殊金币的分值（建议 3）。isSpecialCoin = false 时该值无效")]
        [SerializeField] private int specialScoreValue = 3;

        [Header("Idle Animation")]
        [SerializeField] private float spinSpeed = 90f;
        [SerializeField] private float bobHeight = 0.08f;
        [SerializeField] private float bobSpeed = 1.6f;

        private bool collected;
        private float baseHeight;
        private float bobPhase;

        /// <summary>该金币的分值。</summary>
        public int ScoreValue
        {
            get { return scoreValue; }
        }

        /// <summary>是否已被收集（用于测试与调试）。</summary>
        public bool IsCollected
        {
            get { return collected; }
        }

        /// <summary>是否为特殊金币。</summary>
        public bool IsSpecialCoin
        {
            get { return isSpecialCoin; }
        }

        private void Awake()
        {
            baseHeight = transform.position.y;
            // 用位置生成相位，避免所有金币同步上下浮动
            bobPhase = transform.position.x * 0.37f + transform.position.z * 0.71f;
        }

        private void Update()
        {
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);

            Vector3 position = transform.position;//旋转，绕物理世界的y轴旋转
            position.y = baseHeight + Mathf.Sin((Time.time + bobPhase) * bobSpeed) * bobHeight;
            transform.position = position;//通过正弦函数，不影响x，z轴的情况下，进行y轴变化的旋转
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected || other == null)
            {
                return;//防止同一枚金币被反复拾取
            }

            if (!other.CompareTag("Player"))
            {
                return;//检测是否是玩家
            }

            GameManager manager = GameManager.Instance;//通过此行代码，找到gamemanager
            if (manager == null)
            {
                Debug.LogError("[Coin] 场景中找不到 GameManager，无法计分。请确认 MorandiGameScene 中存在 GameManager 对象。", this);
                return;
            }

            // 只有 Playing 状态才能被收集：胜利或死亡后金币一律不再拾取
            if (!manager.IsPlaying)
            {
                return;
            }

            collected = true;//先标记对象，在当前帧结束前销毁

            if (!isSpecialCoin)
            {
                // 普通金币：保持原有路径，行为与升级前完全一致
                manager.AddScore(scoreValue);
                manager.PlayCoinPickupSound();
                Destroy(gameObject);
                return;
            }

            // ---- 特殊金币 ----
            int value = specialScoreValue;
            Vector3 pickupPosition = transform.position;//首先保存当前金币的位置，用来后期导演镜头抖动或者别的什么特效

            // 先判定这枚是否会把分数直接顶到目标分：用于决定要不要追加追击者，
            // 并在胜利时把胜利面板推迟到演出结束后再弹出
            SpecialPickupDirector director = SpecialPickupDirector.Instance;
            if (director != null)
            {
                director.NotifySpecialPickup(manager.Score + value >= manager.TargetScore);//必须在加分前就预演当前局势是否可以导致胜利，再决定要不要追加第二枚追加者，以及播放惊喜特效
            }

            manager.AddScore(value);

            if (director != null)
            {
                director.PlaySpecialPickup(pickupPosition);
            }

            Destroy(gameObject);
        }
    }
}
