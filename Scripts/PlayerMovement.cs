using UnityEngine;

namespace CoinCollector
{
    /// <summary>
    /// 玩家移动控制器：WASD / 方向键驱动。
    /// 通过 Rigidbody 速度驱动位移，不直接修改 Transform.position，因此不会穿过墙体或障碍物。
    /// 玩家本体是椭圆（非等比缩放球体），旋转对称，因此不需要朝向逻辑；
    /// Rigidbody 冻结旋转，保证碰撞后不会翻倒。
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("玩家最大水平移动速度（米 / 秒）")]
        [SerializeField] private float moveSpeed = 8f;

        [Tooltip("开启后，WASD 方向以摄像机水平朝向为参考；关闭后以世界坐标轴为参考")]
        [SerializeField] private bool cameraRelativeMovement = true;

        [Header("Runtime")]
        [Tooltip("胜利或暂停时会被置为 false，禁止移动")]
        [SerializeField] private bool movementEnabled = true;

        private Rigidbody body;
        private Vector2 moveInput;
        private Vector2? externalInputOverride;

        /// <summary>是否允许移动（胜利后为 false）。</summary>
        public bool MovementEnabled
        {
            get { return movementEnabled; }
        }

        /// <summary>
        /// 外部输入覆盖，用于虚拟摇杆或自动化测试；赋值后优先于键盘输入，置空则恢复键盘。
        /// </summary>
        public Vector2? ExternalInputOverride
        {
            get { return externalInputOverride; }
            set { externalInputOverride = value; }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            ConfigureRigidbody();
        }

        private void Update()
        {
            if (!movementEnabled)
            {
                moveInput = Vector2.zero;
                return;
            }

            if (externalInputOverride.HasValue)
            {
                moveInput = externalInputOverride.Value;
            }
            else
            {
                moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            }

            if (moveInput.sqrMagnitude > 1f)
            {
                moveInput.Normalize();
            }
        }

        private void FixedUpdate()
        {
            if (body == null)
            {
                return;
            }

            Vector3 velocity = body.velocity;

            if (!movementEnabled || moveInput.sqrMagnitude < 0.0001f)
            {
                // 无输入时立刻刹住水平速度，保留重力方向速度
                body.velocity = new Vector3(0f, velocity.y, 0f);
                return;
            }

            Vector3 direction = ResolveMoveDirection(moveInput);
            Vector3 desired = direction * moveSpeed;
            desired.y = velocity.y;
            body.velocity = desired;
        }

        /// <summary>把二维输入解析为世界空间水平方向（只取摄像机朝向的水平投影）。</summary>
        private Vector3 ResolveMoveDirection(Vector2 input)
        {
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;

            Camera camera = Camera.main;
            if (cameraRelativeMovement && camera != null)
            {
                forward = camera.transform.forward;
                forward.y = 0f;
                right = camera.transform.right;
                right.y = 0f;

                if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward; else forward.Normalize();
                if (right.sqrMagnitude < 0.0001f) right = Vector3.right; else right.Normalize();
            }

            Vector3 direction = forward * input.y + right * input.x;
            if (direction.sqrMagnitude > 0.0001f)
            {
                direction.Normalize();
            }
            return direction;
        }

        /// <summary>开启或关闭移动（胜利时调用）。</summary>
        public void SetMovementEnabled(bool enabled)
        {
            movementEnabled = enabled;
            if (!enabled)
            {
                moveInput = Vector2.zero;
                if (body != null)
                {
                    body.velocity = new Vector3(0f, body.velocity.y, 0f);
                }
            }
        }

        private void ConfigureRigidbody()
        {
            if (body == null)
            {
                return;
            }

            body.mass = 1f;
            body.drag = 0f;
            body.angularDrag = 0.05f;
            body.useGravity = true;
            body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            // 冻结全部旋转：椭圆玩家碰撞后不会翻倒或持续自转
            body.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }
}
