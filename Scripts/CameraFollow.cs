using UnityEngine;

namespace CoinCollector
{
    /// <summary>
    /// 关卡摄像机控制。
    /// 默认使用固定斜俯视机位，可以完整看到场地、玩家、金币与障碍物；
    /// 勾选 followTarget 后切换为平滑跟随模式。
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private bool followTarget = false;

        [Header("Framing")]
        [Tooltip("固定机位模式下的世界坐标")]
        [SerializeField] private Vector3 fixedPosition = new Vector3(0f, 26f, -22f);
        [Tooltip("跟随模式下相对目标的偏移")]
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 22f, -16f);
        [SerializeField] private Vector3 lookAtEulerAngles = new Vector3(50f, 0f, 0f);
        [Tooltip("跟随模式下是否始终看向目标")]
        [SerializeField] private bool lookAtTarget = false;
        [SerializeField] private float followSmoothing = 4f;
        [SerializeField] private float fieldOfView = 60f;

        [Header("Shake")]
        [Tooltip("震动最大幅度（米）。0 = 关闭震动（默认，保持原行为）")]
        [SerializeField] private float shakeAmplitude = 0f;
        [Tooltip("震动频率（Hz），越大抖得越快")]
        [SerializeField] private float shakeFrequency = 28f;

        private float shakeDuration = 0.0001f;
        private float shakeTimeRemaining;

        private void Awake()
        {
            ApplyFieldOfView();
        }

        private void Start()
        {
            SnapToDesiredPosition();
        }

        private void LateUpdate()
        {
            // 在写回基础机位之前叠加震动偏移：LateUpdate 每帧都会重写 transform，
            // 因此震动必须由本脚本自己实现，外部改 Camera transform 会被覆盖
            Vector3 shakeOffset = Vector3.zero;
            if (shakeTimeRemaining > 0f)
            {
                shakeTimeRemaining -= Time.deltaTime;
                float falloff = Mathf.Clamp01(shakeTimeRemaining / shakeDuration);
                float amplitude = shakeAmplitude * falloff;
                float t = Time.time * shakeFrequency;
                shakeOffset = new Vector3(
                    Mathf.Sin(t * 1.13f) * amplitude,
                    Mathf.Sin(t * 1.77f + 1.3f) * amplitude * 0.6f,
                    Mathf.Sin(t * 0.91f + 2.1f) * amplitude * 0.4f);
            }

            if (followTarget && target != null)
            {
                Vector3 desired = target.position + followOffset;
                float t = 1f - Mathf.Exp(-followSmoothing * Time.deltaTime);
                transform.position = Vector3.Lerp(transform.position, desired, t) + shakeOffset;

                if (lookAtTarget)
                {
                    transform.LookAt(target.position);
                }
                else
                {
                    transform.rotation = Quaternion.Euler(lookAtEulerAngles);
                }
            }
            else
            {
                transform.position = fixedPosition + shakeOffset;
                transform.rotation = Quaternion.Euler(lookAtEulerAngles);
            }
        }

        /// <summary>
        /// 触发一次摄像机震动。只在 Inspector 中配置了 shakeAmplitude &gt; 0 时生效。
        /// 只影响画面偏移，不触碰玩家移动与输入，操作不受影响。
        /// </summary>
        public void Shake(float duration)
        {
            if (shakeAmplitude <= 0f || duration <= 0f)
            {
                return;
            }

            shakeDuration = duration;
            shakeTimeRemaining = duration;
        }

        private void SnapToDesiredPosition()
        {
            if (followTarget && target != null)
            {
                transform.position = target.position + followOffset;
                if (lookAtTarget)
                {
                    transform.LookAt(target.position);
                }
                else
                {
                    transform.rotation = Quaternion.Euler(lookAtEulerAngles);
                }
            }
            else
            {
                transform.position = fixedPosition;
                transform.rotation = Quaternion.Euler(lookAtEulerAngles);
            }
        }

        private void ApplyFieldOfView()
        {
            Camera camera = GetComponent<Camera>();
            if (camera != null)
            {
                camera.fieldOfView = fieldOfView;
            }
        }
    }
}
