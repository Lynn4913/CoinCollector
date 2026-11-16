using UnityEngine;

namespace CoinCollector
{
    /// <summary>
    /// 椭圆追击者：游戏开始后以明显慢于玩家的速度持续追踪玩家。
    /// 移动通过 Rigidbody 速度驱动，因此会被墙壁与障碍物挡住，不会穿墙。
    /// 只有当追击者的碰撞体与带 Player 标签的对象发生真实物理接触时才触发死亡。
    /// </summary>                                                                                                                      
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class Chaser : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("追击目标（玩家 Transform）；为空时自动查找场景中的 PlayerMovement")]
        [SerializeField] private Transform player;
        [Tooltip("游戏管理器；为空时使用 GameManager.Instance")]
        [SerializeField] private GameManager gameManager;

        [Header("Chase")]
        [Tooltip("追击速度（米 / 秒）。玩家为 8，建议 5.2（约慢 35%）")]
        [SerializeField] private float chaseSpeed = 5.2f;
        [Tooltip("是否允许追击；胜利或死亡后会被置为 false")]
        [SerializeField] private bool chaseEnabled = true;
        [Tooltip("true：用 Rigidbody 速度驱动（会被墙 / 障碍物挡住，推荐）；false：直接改 Transform（可能穿墙）")]
        [SerializeField] private bool useRigidbodyMovement = true;
        [Tooltip("与玩家距离小于此值时停止逼近，避免抖动")]
        [SerializeField] private float stopDistance = 0.15f;

        [Header("Spawn")]
        [Tooltip("Start 时是否把追击者重置到初始位置")]
        [SerializeField] private bool resetToSpawnOnStart = true;
        [Tooltip("初始位置（可玩区域内的远端角落）")]
        [SerializeField] private Vector3 spawnPosition = new Vector3(12f, 0.7f, 12f);

        [Header("Safety")]
        [Tooltip("额外用 Physics.ComputePenetration 做一次真实重叠兜底检查（默认关闭，物理碰撞已足够）")]
        [SerializeField] private bool useContactFallback = false;

        [Header("Temporary (extra chaser only)")]
        [Tooltip("临时追击者存活秒数；<= 0 表示不限时（开局常驻追击者保持默认即可）")]
        [SerializeField] private float temporaryLifetime = 0f;
        [Tooltip("碰到玩家后是否立即自我销毁（临时追击者建议开启；开局常驻追击者保持关闭）")]
        [SerializeField] private bool destroyOnPlayerContact = false;

        private Rigidbody body;
        private SphereCollider sphere;
        private bool chasing;
        private bool deathTriggered;
        private bool hasLifetime;
        private float remainingLifetime;

        /// <summary>当前是否正在追击。</summary>
        public bool IsChasing
        {
            get { return chasing; }
        }

        /// <summary>追击速度（只读，便于测试核对）。</summary>
        public float ChaseSpeed
        {
            get { return chaseSpeed; }
        }

        /// <summary>是否为限时存活的临时追击者。</summary>
        public bool IsTemporary
        {
            get { return hasLifetime; }
        }

        /// <summary>运行时修改追击速度（追加追击者使用；不影响 Inspector 默认值语义）。</summary>
        public void SetChaseSpeed(float speed)
        {
            chaseSpeed = Mathf.Max(0f, speed);
        }

        /// <summary>
        /// 把该实例配置为临时追击者：限时存活 + 撞到玩家后自毁 + 不回出生点。
        /// 只应作用于 Instantiate 出来的新实例，绝不要对开局常驻追击者调用。
        /// </summary>
        public void MakeTemporary(float lifetimeSeconds, float speed)
        {
            SetChaseSpeed(speed);
            resetToSpawnOnStart = false;//追加者在指定位置开始追击，不然就会重叠
            destroyOnPlayerContact = true;
            remainingLifetime = Mathf.Max(0.1f, lifetimeSeconds);
            hasLifetime = true;
        }

        /// <summary>刷新临时追击者的剩余存活时间（重复触发时不叠加数量，只续命）。</summary>
        public void RefreshLifetime(float lifetimeSeconds)
        {
            if (!hasLifetime)
            {
                return;
            }

            remainingLifetime = Mathf.Max(0.1f, lifetimeSeconds);
        }

        private void Awake()//获取组件并且配置物理特性
        {
            body = GetComponent<Rigidbody>();
            sphere = GetComponent<SphereCollider>();
            ConfigureRigidbody();
            deathTriggered = false;
        }

        private void Start()
        {
            if (player == null)
            {
                PlayerMovement movement = Object.FindObjectOfType<PlayerMovement>();//在场景中追击玩家
                if (movement != null)
                {
                    player = movement.transform;
                }
            }

            if (player == null)
            {
                Debug.LogError("[Chaser] 未找到玩家，追击者将保持静止。", this);
            }

            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (resetToSpawnOnStart)
            {
                transform.position = spawnPosition;
            }

            StopMotion();
            chasing = chaseEnabled;
        }

        private void FixedUpdate()
        {
            if (!chasing)
            {
                StopMotion();
                return;
            }

            // 游戏已经结束（胜利或失败）时立即停止追击
            if (gameManager != null && !gameManager.IsPlaying)
            {
                chasing = false;
                StopMotion();
                return;
            }

            if (player == null)
            {
                StopMotion();
                return;
            }

            Vector3 toPlayer = player.position - transform.position;//计算是在玩家身后几米的位置出现
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude <= stopDistance * stopDistance)
            {
                StopMotion();
                return;
            }

            Vector3 direction = toPlayer.normalized;//使追加者不会因为离得远，速度就很快

            if (body != null && useRigidbodyMovement)
            {
                // 速度驱动：碰撞由物理引擎解算，遇到墙 / 障碍物会被挡住
                body.velocity = new Vector3(
                    direction.x * chaseSpeed, 0f, direction.z * chaseSpeed);
            }
            else
            {
                transform.position += direction * chaseSpeed * Time.fixedDeltaTime;
            }
        }

        private void Update()
        {
            // 临时追击者倒计时：归零后自动销毁（开局常驻追击者 hasLifetime = false，不受影响）
            if (hasLifetime)
            {
                remainingLifetime -= Time.deltaTime;//限时临时追击者
                if (remainingLifetime <= 0f)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            if (!useContactFallback || deathTriggered || !chasing)
            {
                return;
            }

            if (player == null || sphere == null)
            {
                return;
            }

            Collider playerCollider = player.GetComponent<Collider>();
            if (playerCollider == null)
            {
                return;
            }

            Vector3 direction;
            float distance;
            bool overlapping = Physics.ComputePenetration(
                sphere, transform.position, transform.rotation,
                playerCollider, player.position, player.rotation,
                out direction, out distance);

            if (overlapping && distance > 0f)
            {
                TriggerDeath();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null || collision.gameObject == null)
            {
                return;
            }

            // 只有与玩家的真实接触才触发死亡；金币、墙、障碍物都会被忽略
            if (!collision.gameObject.CompareTag("Player"))
            {
                return;
            }

            TriggerDeath();
        }

        private void TriggerDeath()
        {
            if (deathTriggered)
            {
                return;
            }

            deathTriggered = true;//防止重复触发

            GameManager manager = gameManager != null ? gameManager : GameManager.Instance;
            if (manager == null)
            {
                Debug.LogError("[Chaser] 场景中找不到 GameManager，无法触发失败。", this);
                return;
            }

            manager.TriggerGameOver();

            // 临时追击者撞到玩家后立即消失（惩罚逻辑复用上面的 TriggerGameOver，本行只负责清理自己）
            if (destroyOnPlayerContact)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>开启 / 停止追击（胜利与死亡时由 GameManager 调用）。</summary>
        public void SetChasing(bool value)
        {
            chasing = value;
            if (!value)
            {
                StopMotion();
            }
        }

        /// <summary>把追击者放回初始位置（Restart 场景重载时自动发生）。</summary>
        public void Respawn()
        {
            transform.position = spawnPosition;
            deathTriggered = false;
            StopMotion();
            chasing = chaseEnabled;
        }

        private void StopMotion()
        {
            if (body != null)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        private void ConfigureRigidbody()
        {
            if (body == null)
            {
                return;
            }

            body.mass = 2f;
            body.drag = 0f;
            body.angularDrag = 0.05f;
            // 关闭重力：追击者悬停在固定高度，不会掉出地面也不会压在地面上抖动
            body.useGravity = false;
            body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezeRotation;

            if (sphere != null)
            {
                // 非等比缩放的球体碰撞体即椭球；必须是实体碰撞体才能产生 OnCollisionEnter
                sphere.isTrigger = false;
            }
        }
    }
}
