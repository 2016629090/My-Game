using UnityEngine;
using VContainer.Unity;

[System.Serializable]
public class PlayerConfig
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    public float rotationSpeed = 10f;

    [Header("跳跃设置")]
    public float jumpForce = 8f;
    public float jumpCooldown = 0.3f;
    public float groundCheckDistance = 0.1f;
    public LayerMask groundLayer = 1; // 默认层
}
public class PlayerController : IStartable, ITickable
{
    private readonly IInputService _inputService;
    private readonly IAnimationService _animationService;
    private readonly Transform _playerTransform;
    private readonly PlayerConfig _config;

    // Rigidbody 相关变量
    private Rigidbody _rigidbody;
    private bool _isGrounded;
    private float _jumpCooldownTimer;
    private bool _isJumping;

    public PlayerController(
        IInputService inputService,
        IAnimationService animationService,
        PlayerConfig config)
    {
        _inputService = inputService;
        _animationService = animationService;
        _config = config;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        _playerTransform = player.transform;
        _rigidbody = player.GetComponent<Rigidbody>();

        if (_rigidbody == null)
        {
            Debug.LogError("PlayerController: Rigidbody component not found on player!");
        }
    }

    public void Tick()
    {
        CheckGrounded();
        HandleJump();
        HandleMovement();
        UpdateAnimations();
    }

    private void CheckGrounded()
    {
        // 使用射线检测是否着地
        RaycastHit hit;
        Vector3 rayStart = _playerTransform.position + Vector3.up * 0.1f;
        bool hitGround = Physics.Raycast(rayStart, Vector3.down, out hit,
            _config.groundCheckDistance, _config.groundLayer);

        _isGrounded = hitGround;

        // 如果正在跳跃但已经着地，重置跳跃状态
        if (_isGrounded && _isJumping && _rigidbody.velocity.y <= 0)
        {
            _isJumping = false;
        }
    }

    private void HandleJump()
    {
        // 处理跳跃冷却
        if (_jumpCooldownTimer > 0)
        {
            _jumpCooldownTimer -= Time.deltaTime;
        }

        // 检测跳跃输入
        if (_inputService.IsJumpPressed() && _isGrounded && _jumpCooldownTimer <= 0)
        {
            Jump();
        }
    }

    private void HandleMovement()
    {
        // 获取输入
        float horizontal = _inputService.GetHorizontal();
        float vertical = _inputService.GetVertical();

        // 基于相机方向计算移动
        Vector3 cameraForward = _inputService.GetCameraForward();
        Vector3 cameraRight = _inputService.GetCameraRight();

        Vector3 movement = (cameraForward * vertical + cameraRight * horizontal).normalized;

        if (movement.magnitude > 0.1f)
        {
            // 计算移动速度
            float currentSpeed = _inputService.IsRunning() ? _config.runSpeed : _config.moveSpeed;

            // 方法1：使用 AddForce（推荐）
            Vector3 moveForce = movement * currentSpeed * 10f; // 乘以系数调整力度
            _rigidbody.AddForce(moveForce, ForceMode.Force);

            // 或者方法2：只修改水平速度，保持垂直速度
            // Vector3 velocity = movement * currentSpeed;
            // velocity.y = _rigidbody.velocity.y;
            // _rigidbody.velocity = velocity;

            // 面向移动方向
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            _playerTransform.rotation = Quaternion.Slerp(
                _playerTransform.rotation,
                targetRotation,
                Time.deltaTime * _config.rotationSpeed
            );
        }
        else
        {
            // 没有输入时，应用阻力来减速
            Vector3 velocity = _rigidbody.velocity;
            velocity.x *= 0.9f;
            velocity.z *= 0.9f;
            _rigidbody.velocity = velocity;
        }
    }

    private void UpdateAnimations()
    {
        float horizontal = _inputService.GetHorizontal();
        float vertical = _inputService.GetVertical();

        _animationService.UpdateMovementAnimation(horizontal, vertical, _inputService.IsRunning());
        _animationService.SetGrounded(_isGrounded);
    }

    private void Jump()
    {
        // 首先确保Y轴速度为0，避免累积跳跃
        Vector3 velocity = _rigidbody.velocity;
        velocity.y = 0;
        _rigidbody.velocity = velocity;

        // 应用跳跃力
        _rigidbody.AddForce(Vector3.up * _config.jumpForce, ForceMode.Impulse);

        // 触发动画
        _animationService.TriggerJump();

        // 设置状态和冷却
        _isJumping = true;
        _jumpCooldownTimer = _config.jumpCooldown;
    }

    public void Start() { }
    public void Dispose() { }
}