using VContainer;
using VContainer.Unity;
using UnityEngine;

public class GameLifetimeScope : LifetimeScope
{
    [SerializeField] private Animator playerAnimator; // 在Inspector中拖拽赋值

    protected override void Configure(IContainerBuilder builder)
    {
        // 注册动画服务
        builder.Register<AnimationService>(Lifetime.Singleton)
               .As<IAnimationService>();

        // 注册输入服务
        builder.Register<InputService>(Lifetime.Singleton)
               .As<IInputService>();

        // 注册玩家控制器
        builder.RegisterEntryPoint<PlayerController>(Lifetime.Scoped);

        // 注册Animator（通过Inspector赋值）
        builder.RegisterInstance(playerAnimator);
    }
}