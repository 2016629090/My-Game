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
        walking,
        Runing,
        Jumping
    }
    [System.Serializable]
    public class MovementSetting
    {
        [Header("基础速度")]
        public float walkSpeed = 2f;
        public float runSpeed = 5f;
    }

    [System.Serializable]
    public class InputSetting
    {
        public string horizontalAxis = "Horizontal";
        public string verticalAxis = "Vertical";
        public KeyCode jumpkey = KeyCode.Space;
        public float inputBufferTime = 0.1f;
    }

    [System.Serializable]
    public class AnimationSetting
    {
        public Animator animator;
        public string speedParameter = "Speed";
        public string jumpTrigger = "Jump";
        public string isGroundedParameter = "IsGrounded";
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

        private Vector3 moveDirection;
        private float verticalVelocity;
        private MoveState currentState = MoveState.Idle;
        private bool isGrounded;

        // 动画参数缓存
        private int speedHash;
        private int jumpHash;
        private int groundedHash;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            InitializeAnimatorParameters();
        }

        #region 初始化
        private void InitializeComponents()
        {
            if(characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            if(cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if(animationSetting.animator == null)
            {
                animationSetting.animator = GetComponent<Animator>();
            }
        }

        private void InitializeAnimatorParameters()
        {
            // 缓存Animator参数哈希值，提升性能
            speedHash = Animator.StringToHash(animationSetting.speedParameter);
            jumpHash = Animator.StringToHash(animationSetting.jumpTrigger);
            groundedHash = Animator.StringToHash(animationSetting.isGroundedParameter);
        }

        #endregion

        #region 地面检测

        private void HandleGroundCheck()
        {
            // 使用CharacterController自带的地面检测
            isGrounded = characterController.isGrounded;

            // 如果在地面上且垂直速度为负，重置垂直速度
            if (isGrounded && verticalVelocity < 0)
            {
                verticalVelocity = -2f; // 小负值确保贴地
            }
        }

        #endregion

        private void HandleMovement()
        {
            float horizontal = Input.GetAxisRaw(inputSetting.horizontalAxis);
            float vertical = Input.GetAxisRaw(inputSetting.verticalAxis);
            Vector2 input = new Vector2(horizontal, vertical).normalized;

            // 如果没有输入，重置移动方向
            if (input.magnitude < 0.1f)
            {
                moveDirection = Vector3.zero;
            }

            // 计算相机相对移动方向
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;
            // 忽略相机的上下倾斜，只在水平面移动
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();

        }
    }
}
