using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YY.RPGgame
{
    public enum PlayerMoveDirect
    {
        Forward,
        Back,
        Left,
        Right
    }

    public enum MoveState
    {
        Idle,
        Walking,
        Running,
        Jumping,
        Falling
    }

    [System.Serializable]
    public class MovementSetting
    {
        [Header("基础速度")]
        public float walkSpeed = 2f;
        public float runSpeed = 5f;
        public float jumpHeight = 2f;
        public float gravity = -9.81f;
        public float rotationSpeed = 10f;

        [Header("地面检测")]
        public float groundCheckDistance = 0.2f;
        public float groundCheckRadius = 0.3f;
        public LayerMask groundLayer = -1;
    }

    [System.Serializable]
    public class InputSetting
    {
        public string horizontalAxis = "Horizontal";
        public string verticalAxis = "Vertical";
        public KeyCode jumpKey = KeyCode.Space;
        public KeyCode runKey = KeyCode.LeftShift;
        public float jumpBufferTime = 0.1f;
    }

    [System.Serializable]
    public class AnimationSetting
    {
        public Animator animator;
        public string speedParameter = "Speed";
        public string jumpTrigger = "Jump";
        public string isGroundedParameter = "IsGrounded";
        public string verticalVelocityParameter = "VerticalVelocity";
    }

    public class PlayerMove : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform cameraTransform;

        [Header("配置")]
        [SerializeField] private MovementSetting movementSetting = new MovementSetting();
        [SerializeField] private InputSetting inputSetting = new InputSetting();
        [SerializeField] private AnimationSetting animationSetting = new AnimationSetting();

        // 移动变量
        private Vector3 moveDirection;
        private float verticalVelocity;
        private MoveState currentState = MoveState.Idle;
        private bool isGrounded;
        private bool wasGrounded;
        private float lastTimeGrounded;

        // 动画参数缓存
        private int speedHash;
        private int jumpHash;
        private int groundedHash;
        private int verticalVelocityHash;

        // 输入缓冲
        private float jumpBufferTimer;
        private bool jumpRequested;

        // 跳跃状态控制
        private bool isJumping = false;
        private float jumpStartTime;
        private float jumpCoyoteTime = 0.15f; // 跳跃宽容时间

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            InitializeAnimatorParameters();
        }

        private void Update()
        {
            HandleInput();
            HandleGroundCheck();
            HandleMovement();
            HandleJump();
            HandleGravity();
            UpdateState();
            UpdateAnimations();
        }

        #region 初始化

        private void InitializeComponents()
        {
            // 获取CharacterController
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
                if (characterController == null)
                {
                    characterController = gameObject.AddComponent<CharacterController>();
                    characterController.minMoveDistance = 0.001f; // 减少微小移动
                }
            }

            // 获取相机引用
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            // 获取动画器
            if (animationSetting.animator == null)
            {
                animationSetting.animator = GetComponentInChildren<Animator>();
            }
        }

        private void InitializeAnimatorParameters()
        {
            // 缓存Animator参数哈希值，提升性能
            speedHash = Animator.StringToHash(animationSetting.speedParameter);
            jumpHash = Animator.StringToHash(animationSetting.jumpTrigger);
            groundedHash = Animator.StringToHash(animationSetting.isGroundedParameter);
            verticalVelocityHash = Animator.StringToHash(animationSetting.verticalVelocityParameter);
        }

        #endregion

        #region 输入处理

        private void HandleInput()
        {
            // 处理跳跃输入缓冲
            if (Input.GetKeyDown(inputSetting.jumpKey))
            {
                jumpRequested = true;
                jumpBufferTimer = inputSetting.jumpBufferTime;
            }

            // 减少缓冲计时器
            if (jumpBufferTimer > 0)
            {
                jumpBufferTimer -= Time.deltaTime;
            }
            else
            {
                jumpRequested = false;
            }
        }

        #endregion

        #region 地面检测（修复版本）

        private void HandleGroundCheck()
        {
            wasGrounded = isGrounded;

            // 方法1：使用CharacterController.isGrounded（但不完全可靠）
            isGrounded = characterController.isGrounded;

            // 方法2：使用物理射线检测作为补充（更精确）
            Vector3 checkPosition = transform.position + Vector3.up * 0.1f; // 稍微抬高检测点
            bool raycastGrounded = Physics.CheckSphere(
                checkPosition,
                movementSetting.groundCheckRadius,
                movementSetting.groundLayer,
                QueryTriggerInteraction.Ignore
            );

            // 如果两种检测结果不一致，使用更保守的判断
            if (raycastGrounded && !isGrounded)
            {
                // 如果物理检测到地面但CharacterController没检测到，以物理检测为准
                isGrounded = true;
            }
            else if (!raycastGrounded && isGrounded)
            {
                // 如果物理没检测到地面但CharacterController检测到了，可能需要进一步验证
                // 这里我们可以再做一个向下射线检测
                if (Physics.Raycast(checkPosition, Vector3.down,
                    movementSetting.groundCheckDistance + 0.1f, movementSetting.groundLayer))
                {
                    isGrounded = true;
                }
            }

            // 记录最近一次着地时间（用于跳跃宽容期）
            if (isGrounded)
            {
                lastTimeGrounded = Time.time;

                // 如果刚刚着地且之前是跳跃状态，结束跳跃
                if (isJumping && verticalVelocity <= 0)
                {
                    isJumping = false;
                }
            }

            // 处理垂直速度为负时的地面检测（确保贴地）
            if (isGrounded && verticalVelocity < 0)
            {
                verticalVelocity = -2f;
            }
        }

        #endregion

        #region 移动处理

        private void HandleMovement()
        {
            // 获取输入
            float horizontal = Input.GetAxisRaw(inputSetting.horizontalAxis);
            float vertical = Input.GetAxisRaw(inputSetting.verticalAxis);
            Vector2 input = new Vector2(horizontal, vertical).normalized;

            // 如果没有输入，重置移动方向
            if (input.magnitude < 0.1f)
            {
                moveDirection = Vector3.zero;
                return;
            }

            // 计算相机相对移动方向
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;

            // 忽略相机的上下倾斜，只在水平面移动
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();

            // 计算移动方向
            moveDirection = cameraForward * input.y + cameraRight * input.x;
            moveDirection.Normalize();

            // 确定速度
            float speed = Input.GetKey(inputSetting.runKey) ?
                movementSetting.runSpeed : movementSetting.walkSpeed;

            // 处理角色旋转（面向移动方向）
            if (moveDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    movementSetting.rotationSpeed * Time.deltaTime
                );
            }

            // 应用移动（水平方向）
            Vector3 horizontalVelocity = moveDirection * speed;

            // 应用移动（包含垂直速度）
            Vector3 finalVelocity = new Vector3(
                horizontalVelocity.x,
                verticalVelocity,
                horizontalVelocity.z
            );

            characterController.Move(finalVelocity * Time.deltaTime);
        }

        #endregion

        #region 跳跃处理（修复版本）

        private void HandleJump()
        {
            // 判断是否可以跳跃（使用宽容期）
            bool canJump = isGrounded || (Time.time - lastTimeGrounded <= jumpCoyoteTime);

            // 如果有跳跃请求并且可以跳跃
            if (jumpRequested && canJump && !isJumping)
            {
                // 计算跳跃初速度
                verticalVelocity = Mathf.Sqrt(movementSetting.jumpHeight * -2f * movementSetting.gravity);

                // 设置跳跃状态
                isJumping = true;
                jumpStartTime = Time.time;

                // 清除跳跃请求
                jumpRequested = false;
                jumpBufferTimer = 0;

                // 触发跳跃动画
                if (animationSetting.animator != null)
                {
                    animationSetting.animator.SetTrigger(jumpHash);
                }
            }
        }

        #endregion

        #region 重力处理

        private void HandleGravity()
        {
            // 应用重力
            if (!isGrounded)
            {
                verticalVelocity += movementSetting.gravity * Time.deltaTime;

                // 限制最大下落速度
                if (verticalVelocity < -20f)
                {
                    verticalVelocity = -20f;
                }
            }
        }

        #endregion

        #region 状态更新（修复版本）

        private void UpdateState()
        {
            // 保存之前的状态
            MoveState previousState = currentState;

            // 如果正在跳跃
            if (isJumping)
            {
                // 如果还在上升阶段
                if (verticalVelocity > 0)
                {
                    currentState = MoveState.Jumping;
                }
                // 如果开始下落
                else if (!isGrounded)
                {
                    currentState = MoveState.Falling;
                }
                // 如果已经着地
                else if (isGrounded)
                {
                    // 跳跃结束，根据输入决定下一个状态
                    isJumping = false;
                }
            }

            // 如果不是跳跃状态
            if (!isJumping)
            {
                if (isGrounded)
                {
                    // 如果有移动输入
                    if (moveDirection.magnitude > 0.1f)
                    {
                        currentState = Input.GetKey(inputSetting.runKey) ?
                            MoveState.Running : MoveState.Walking;
                    }
                    else
                    {
                        currentState = MoveState.Idle;
                    }
                }
                else
                {
                    // 不在地面且不是跳跃状态，就是下落状态
                    currentState = MoveState.Falling;
                }
            }
        }

        #endregion

        #region 动画更新（修复版本）

        private void UpdateAnimations()
        {
            if (animationSetting.animator == null) return;

            // 方法1：基于输入强度和状态计算（最稳定）
            float speedValue = 0f;

            // 获取输入强度（使用平滑输入）
            float horizontal = Input.GetAxis(inputSetting.horizontalAxis); // 改为GetAxis
            float vertical = Input.GetAxis(inputSetting.verticalAxis);     // 改为GetAxis
            float inputMagnitude = Mathf.Clamp01(new Vector2(horizontal, vertical).magnitude);

            // 根据状态计算动画速度
            if (isGrounded)
            {
                if (inputMagnitude > 0.1f)
                {
                    if (Input.GetKey(inputSetting.runKey))
                    {
                        // 奔跑状态：0.5-1.0
                        speedValue = 0.5f + inputMagnitude * 0.5f;
                    }
                    else
                    {
                        // 行走状态：0-0.5
                        speedValue = inputMagnitude * 0.5f;
                    }
                }
                else
                {
                    // 静止状态
                    speedValue = 0f;
                }
            }
            else
            {
                // 空中状态：固定值或根据移动计算
                if (moveDirection.magnitude > 0.1f)
                {
                    speedValue = 0.3f;
                }
                else
                {
                    speedValue = 0.1f;
                }
            }

            // 设置动画参数
            animationSetting.animator.SetFloat(speedHash, speedValue, 0.1f, Time.deltaTime);
            animationSetting.animator.SetBool(groundedHash, isGrounded);
            animationSetting.animator.SetFloat(verticalVelocityHash, verticalVelocity);
        }

        #endregion

        #region 公共方法

        public void Move(PlayerMoveDirect direction, float speedMultiplier = 1f)
        {
            if (cameraTransform == null) return;

            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;

            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();

            switch (direction)
            {
                case PlayerMoveDirect.Forward:
                    moveDirection = cameraForward;
                    break;
                case PlayerMoveDirect.Back:
                    moveDirection = -cameraForward;
                    break;
                case PlayerMoveDirect.Left:
                    moveDirection = -cameraRight;
                    break;
                case PlayerMoveDirect.Right:
                    moveDirection = cameraRight;
                    break;
            }

            moveDirection.Normalize();

            // 应用移动
            float speed = movementSetting.walkSpeed * speedMultiplier;
            Vector3 finalVelocity = new Vector3(
                moveDirection.x * speed,
                verticalVelocity,
                moveDirection.z * speed
            );

            characterController.Move(finalVelocity * Time.deltaTime);
        }

        public void Jump()
        {
            if (isGrounded || (Time.time - lastTimeGrounded <= jumpCoyoteTime))
            {
                verticalVelocity = Mathf.Sqrt(movementSetting.jumpHeight * -2f * movementSetting.gravity);
                isJumping = true;
                jumpStartTime = Time.time;

                if (animationSetting.animator != null)
                {
                    animationSetting.animator.SetTrigger(jumpHash);
                }
            }
        }

        public void SetMoveSpeed(float newWalkSpeed, float newRunSpeed = -1f)
        {
            movementSetting.walkSpeed = newWalkSpeed;
            if (newRunSpeed > 0)
            {
                movementSetting.runSpeed = newRunSpeed;
            }
        }

        public bool IsMoving()
        {
            return moveDirection.magnitude > 0.1f;
        }

        public MoveState GetCurrentState()
        {
            return currentState;
        }

        #endregion

        #region 调试辅助

        private void OnDrawGizmosSelected()
        {
            if (characterController != null)
            {
                // 绘制CharacterController范围
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, characterController.radius);

                // 绘制地面检测范围
                Gizmos.color = isGrounded ? Color.green : Color.red;
                Vector3 checkPosition = transform.position + Vector3.up * 0.1f;
                Gizmos.DrawWireSphere(checkPosition, movementSetting.groundCheckRadius);

                // 绘制移动方向
                if (Application.isPlaying && moveDirection.magnitude > 0.1f)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(transform.position, moveDirection * 2f);
                }
            }
        }

        #endregion
    }
}