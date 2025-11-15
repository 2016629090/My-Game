using UnityEngine;
using VContainer.Unity;
using VContainer;

public class GameLifetimeScope : LifetimeScope
{
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private PlayerConfig playerConfig; // ÔÚInspectorÖĞÅäÖÃ

    protected override void Configure(IContainerBuilder builder)
    {
        // ×¢²á·şÎñ
        builder.Register<AnimationService>(Lifetime.Singleton)
               .As<IAnimationService>();
        builder.Register<InputService>(Lifetime.Singleton)
               .As<IInputService>();

        // ×¢²áÅäÖÃÀàÊµÀı
        builder.RegisterInstance(playerConfig);

        // ×¢²áPlayerController
        builder.RegisterEntryPoint<PlayerController>(Lifetime.Scoped);

        // ×¢²áAnimator
        builder.RegisterInstance(playerAnimator);
    }
}