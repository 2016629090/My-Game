using UnityEngine;

public interface IAnimationService
{
    void UpdateMovementAnimation(float horizontal, float vertical, bool isRunning);
}

public class AnimationService : IAnimationService
{
    private readonly Animator _animator;

    // 通过构造器注入Animator
    public AnimationService(Animator animator)
    {
        _animator = animator;
    }

    public void UpdateMovementAnimation(float horizontal, float vertical, bool isRunning)
    {
        // 设置基础移动参数
        _animator.SetFloat("Horizontal", horizontal);
        _animator.SetFloat("Vertical", vertical);

        // 计算移动速度（用于混合树）
        float speed = Mathf.Max(Mathf.Abs(horizontal), Mathf.Abs(vertical));

        // 跑步逻辑
        if (isRunning && speed > 0.1f)
        {
            speed *= 2f; // 跑步时速度加倍
        }

        _animator.SetFloat("Speed", speed);

        // 设置是否移动状态
        _animator.SetBool("IsMoving", speed > 0.1f);
    }
}