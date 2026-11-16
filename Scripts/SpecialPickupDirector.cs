using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CoinCollector
{
    /// <summary>
    /// 特殊金币演出导演：统一处理特殊金币的音效、摄像机震动、中央大字、粒子特效与追加追击者。
    /// 挂在场景级对象上（不要挂在会被销毁的金币下面），由 SceneUpgrader 自动创建与绑定。
    /// 演出均不暂停游戏、不阻塞玩家移动；胜利面板由 GameManager 负责延迟弹出，两者不会重叠。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]//特殊金币特殊音效
    public class SpecialPickupDirector : MonoBehaviour
    {
        private static SpecialPickupDirector instance;

        public static SpecialPickupDirector Instance
        {
            get { return instance; }
        }

        [Header("Audio")]
        [Tooltip("特殊金币独立音效（不要复用普通金币音效）")]
        [SerializeField] private AudioClip specialPickupClip;
        [Tooltip("特殊音效音量 0~1")]
        [SerializeField] private float specialPickupVolume = 0.9f;

        [Header("Camera Shake")]
        [Tooltip("震动时长（秒），建议 0.2")]
        [SerializeField] private float cameraShakeDuration = 0.2f;
        [Tooltip("摄像机 CameraFollow 引用；为空时自动查找主摄像机")]

        [SerializeField] private CameraFollow cameraFollow;

        [Header("Center Banner")]
        [Tooltip("屏幕中央大字（Image，使用预先渲染好的中文贴图）；为空则跳过大字演出")]
        [SerializeField] private Image bannerImage;
        [Tooltip("大字完全显示的时长（秒），建议 0.6~0.8")]
        [SerializeField] private float bannerDisplaySeconds = 0.7f;
        [Tooltip("大字淡出时长（秒）")]
        [SerializeField] private float bannerFadeSeconds = 0.25f;

        [Header("Particle FX")]
        [Tooltip("粒子特效 Prefab（独立对象，自动销毁，绝不挂在金币下面）；为空时使用内置简易特效兜底")]
        [SerializeField] private ParticleSystem specialFxPrefab;
        [Tooltip("特效生成后自动销毁的等待秒数")]
        [SerializeField] private float fxLifetimeSeconds = 2.5f;

        [Header("Extra Chaser")]
        [Tooltip("追加追击者使用的 Prefab（复用现有 Chaser 组件；为空时克隆场景中的常驻追击者兜底）")]
        [SerializeField] private Chaser chaserPrefab;
        [Tooltip("追加追击者速度 = 常驻追击者速度 × 该倍率（建议 1.0~1.2）")]
        [SerializeField] private float extraChaserSpeedMultiplier = 1.0f;
        [Tooltip("找不到常驻追击者时使用的兜底速度")]
        [SerializeField] private float extraChaserSpeedFallback = 5.2f;
        [Tooltip("追加追击者存活秒数，到时自动销毁（约 8 秒）")]
        [SerializeField] private float extraChaserLifetimeSeconds = 8f;
        [Tooltip("距离玩家的生成距离（米）")]
        [SerializeField] private float extraChaserSpawnDistance = 9f;
        [Tooltip("追加追击者生成高度")]
        [SerializeField] private float extraChaserSpawnHeight = 0.7f;
        [Tooltip("可玩区域半边长，用于把生成点夹在墙内（墙在 ±16.5，留 1 米余量）")]
        [SerializeField] private float arenaHalfSize = 16f;

        private AudioSource audioSource;
        private Chaser extraChaserInstance;
        private bool lastPickupWillReachTarget;
        private Coroutine bannerRoutine;

        private void Awake()//与下面那个完成了单例的建立和清理
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>
        /// 由 Coin 在加分【之前】调用：告知这枚特殊金币是否会直接触发胜利，
        /// 用于决定"只演出"还是"演出 + 追加追击者"。
        /// </summary>
        public void NotifySpecialPickup(bool willReachTarget)
        {
            lastPickupWillReachTarget = willReachTarget;

            // 若这枚金币直接顶到目标分：把胜利面板推迟到演出结束之后，避免两个 UI 重叠
            if (willReachTarget && GameManager.Instance != null)
            {
                float delay = bannerDisplaySeconds + bannerFadeSeconds + 0.05f;//假设玩家当前有 7 分，吃到特殊金币后达到 10 分。

          //这时会发生两件事：
        // 1. 特殊金币播放中央大字；
        // 2. GameManager 触发胜利，准备显示胜利面板。所以把胜利面板进行时间上的推迟，此时已经进入了victory的模式，玩家也动不了
                GameManager.Instance.SetPendingVictoryUiDelay(delay);
            }
        }

        /// <summary>播放特殊金币的完整演出。由 Coin 在加分之后、金币销毁前调用。</summary>
        public void PlaySpecialPickup(Vector3 position)
        {
            PlaySpecialSound();
            PlayCameraShake();
            PlayBanner();
            SpawnFx(position);

            // 只有"未达到目标分数"时才追加追击者；已达到目标分则只演出，
            // 避免胜利后出现无意义的追击者
            if (!lastPickupWillReachTarget)
            {
                SpawnOrRefreshExtraChaser();
            }
        }

        private void PlaySpecialSound()//特殊音效
        {
            if (audioSource == null || specialPickupClip == null)
            {
                Debug.LogWarning("[SpecialPickupDirector] 特殊音效尚未配置（Special Pick Clip），跳过。", this);
                return;
            }

            audioSource.PlayOneShot(specialPickupClip, specialPickupVolume);//播放一次音效
        }

        private void PlayCameraShake()
        {
            if (cameraFollow == null)//是否绑定摄像机
            {
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    cameraFollow = mainCamera.GetComponent<CameraFollow>();
                }
            }

            if (cameraFollow != null)
            {
                cameraFollow.Shake(cameraShakeDuration);//震动摄像机，默认为0.2秒
            }
        }

        private void PlayBanner()//中央大字播报
        {
            if (bannerImage == null)//预先准备好的文字问题，减少渲染调节字体等繁琐工作
            {
                Debug.LogWarning("[SpecialPickupDirector] 中央大字（Banner Image）尚未配置，跳过大字演出。", this);
                return;
            }

            if (bannerRoutine != null)
            {
                StopCoroutine(bannerRoutine);
            }

            bannerRoutine = StartCoroutine(BannerRoutine());//协程可以让一个方法分阶段执行，中间可以等待一段时间，再继续执行，如果出现新的控制到图片的情况，避免打架
        }

        private IEnumerator BannerRoutine()//大字为何出现又消失
        {
            bannerImage.gameObject.SetActive(true);
            SetBannerAlpha(1f);//第一步：设置为透明度为1的完全显示

            yield return new WaitForSeconds(bannerDisplaySeconds);//等待指定秒数，再继续协程

            float elapsed = 0f;
            while (elapsed < bannerFadeSeconds)
            {
                elapsed += Time.deltaTime;
                SetBannerAlpha(1f - Mathf.Clamp01(elapsed / bannerFadeSeconds));//一个数学公式，每一帧都在调整透明度
                yield return null;
            }

            SetBannerAlpha(0f);//专门修改透明度的方法
            bannerImage.gameObject.SetActive(false);
            bannerRoutine = null;
        }

        private void SetBannerAlpha(float alpha)
        {
            Color color = bannerImage.color;
            color.a = alpha;
            bannerImage.color = color;
        }

        private void SpawnFx(Vector3 position)//粒子特效组件
        {
            ParticleSystem fx;
            if (specialFxPrefab != null)
            {
                fx = Instantiate(specialFxPrefab, position, Quaternion.identity);//单独实例化粒子特效，如果直接挂在金币下，金币销毁，特效也销毁，无法单独展示
            }
            else
            {
                Debug.LogWarning("[SpecialPickupDirector] 特效 Prefab 未配置，使用内置简易特效兜底。", this);
                fx = CreateFallbackFx(position);
            }

            fx.name = "FX_SpecialCoin_Runtime";
            Destroy(fx.gameObject, fxLifetimeSeconds);//粒子展示几秒钟后销毁，避免大量播报对象
        }

        /// <summary>生成（首次）或刷新（重复触发）追加追击者：永远只有一个实例，不无限叠加。</summary>
        private void SpawnOrRefreshExtraChaser()//追加追击者
        {
            float speed = ResolveExtraChaserSpeed();

            if (extraChaserInstance != null)
            {
                // 已存在：只刷新持续时间与速度，不增加数量
                extraChaserInstance.RefreshLifetime(extraChaserLifetimeSeconds);
                extraChaserInstance.SetChaseSpeed(speed);
                return;
            }

            GameObject source = chaserPrefab != null ? chaserPrefab.gameObject : ResolveBaseChaserGameObject();//如果没有单独在prefab里面设置追加者，则复制场景中已经有的
            if (source == null)
            {
                Debug.LogError("[SpecialPickupDirector] 既没有 Chaser Prefab，也找不到场景中的追击者，无法生成追加追击者。", this);
                return;
            }

            GameObject clone = Instantiate(source, ComputeSpawnPosition(), Quaternion.identity);//实例化克隆追击者，放在计算好的位置
            clone.name = "ExtraChaser_Temporary";
            extraChaserInstance = clone.GetComponent<Chaser>();//复用chaser的逻辑
            if (extraChaserInstance == null)
            {
                extraChaserInstance = clone.AddComponent<Chaser>();
            }

            // 只对克隆实例配置临时生命周期，绝不触碰开局常驻追击者
            extraChaserInstance.MakeTemporary(extraChaserLifetimeSeconds, speed);
        }

        private float ResolveExtraChaserSpeed()//计算追加者的速度：优先参考常见追击者的速度
        {
            float baseSpeed = 0f;
            Chaser baseChaser = GameManager.Instance != null ? GameManager.Instance.Chaser : null;//寻找原来的追击者
            if (baseChaser == null)
            {
                baseChaser = FindObjectOfType<Chaser>();
            }

            if (baseChaser != null && !baseChaser.IsTemporary)
            {
                baseSpeed = baseChaser.ChaseSpeed;
            }

            if (baseSpeed <= 0f)
            {
                baseSpeed = extraChaserSpeedFallback;
            }

            return baseSpeed * Mathf.Max(0.1f, extraChaserSpeedMultiplier);//避免出现零速率
        }

        private GameObject ResolveBaseChaserGameObject()
        {
            Chaser baseChaser = GameManager.Instance != null ? GameManager.Instance.Chaser : null;
            if (baseChaser == null)
            {
                baseChaser = FindObjectOfType<Chaser>();
            }

            return baseChaser != null ? baseChaser.gameObject : null;
        }

        /// <summary>生成点：玩家移动方向的反方向（背后），夹在可玩区域内，避免直接糊脸。</summary>
        private Vector3 ComputeSpawnPosition()
        {
            PlayerMovement player = FindObjectOfType<PlayerMovement>();
            if (player == null)
            {
                return new Vector3(0f, extraChaserSpawnHeight, 0f);
            }

            Vector3 direction = Vector3.back;//在玩家后侧生成一个追击者
            Rigidbody playerBody = player.GetComponent<Rigidbody>();
            if (playerBody != null && playerBody.velocity.sqrMagnitude > 0.01f)
            {
                direction = playerBody.velocity.normalized;
            }
            else
            {
                // 玩家静止时：取与常驻追击者相反的一侧，避免两面夹击
                Chaser baseChaser = GameManager.Instance != null ? GameManager.Instance.Chaser : null;
                if (baseChaser != null)
                {
                    Vector3 away = player.transform.position - baseChaser.transform.position;
                    away.y = 0f;
                    if (away.sqrMagnitude > 0.01f)
                    {
                        direction = away.normalized;
                    }
                }
            }

            Vector3 position = player.transform.position - direction * extraChaserSpawnDistance;//计算是在玩家身后几米的位置出现
            float limit = Mathf.Max(1f, arenaHalfSize - 1f);
            position.x = Mathf.Clamp(position.x, -limit, limit);
            position.z = Mathf.Clamp(position.z, -limit, limit);
            position.y = extraChaserSpawnHeight;
            return position;
        }

        /// <summary>运行时兜底特效：未配置 Prefab 时生成一个简易金色爆发粒子。</summary>
        private ParticleSystem CreateFallbackFx(Vector3 position)
        {
            GameObject fxObject = new GameObject("FX_SpecialCoin_Fallback");
            fxObject.transform.position = position;

            ParticleSystem fx = fxObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = fx.main;
            main.duration = 1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = 3.5f;
            main.startSize = 0.22f;
            main.startColor = new Color(1f, 0.85f, 0.35f, 1f);
            main.gravityModifier = 0.15f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = fx.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 80) });

            ParticleSystem.ShapeModule shape = fx.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;

            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Legacy Shaders/Particles/Additive");
            }
            if (shader != null)
            {
                ParticleSystemRenderer renderer = fxObject.GetComponent<ParticleSystemRenderer>();
                renderer.material = new Material(shader);
            }

            return fx;
        }
    }
}
