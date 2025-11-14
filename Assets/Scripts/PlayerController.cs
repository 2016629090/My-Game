using UnityEngine;
using VContainer.Unity;

public class PlayerController : IStartable, ITickable, System.IDisposable
{
    private readonly IInputService _inputService;
    private readonly IAnimationService _animationService;
    private readonly Transform _playerTransform;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;

    public PlayerController(IInputService inputService, IAnimationService animationService)
    {
        _inputService = inputService;
        _animationService = animationService;

        // 查找玩家Transform（简化版，实际应该通过依赖注入）
        _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    public void Start()
    {
        Debug.Log("PlayerController Started");
    }

    public void Tick()
    {
        HandleMovement();
        UpdateAnimation();
    }

    private void HandleMovement()
    {
        float horizontal = _inputService.GetHorizontal();
        float vertical = _inputService.GetVertical();

        // 创建移动方向向量
        Vector3 movement = new Vector3(horizontal, 0f, vertical);

        // 如果玩家在移动
        if (movement.magnitude > 0.1f)
        {
            // 标准化移动向量并应用速度
            movement.Normalize();

            float currentSpeed = _inputService.IsRunning() ? runSpeed : moveSpeed;
            movement *= currentSpeed * Time.deltaTime;

            // 应用移动
            _playerTransform.Translate(movement, Space.World);

            // 让角色面向移动方向
            if (movement != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(movement);
                _playerTransform.rotation = Quaternion.Slerp(_playerTransform.rotation, targetRotation, Time.deltaTime * 10f);
            }
        }
    }

    private void UpdateAnimation()
    {
        float horizontal = _inputService.GetHorizontal();
        float vertical = _inputService.GetVertical();
        bool isRunning = _inputService.IsRunning();

        _animationService.UpdateMovementAnimation(horizontal, vertical, isRunning);
    }

    public void Dispose()
    {
        Debug.Log("PlayerController Disposed");
    }
}