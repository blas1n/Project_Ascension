using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;
using ProjectAscension.GameSimulation.Player;

namespace ProjectAscension.Player
{
    /// <summary>
    /// Frontier scene scope. Wires the player stack. Parents to RootLifetimeScope
    /// when launched via Bootstrap, or acts as its own root when Frontier_01 is
    /// played directly.
    /// </summary>
    public sealed class FrontierLifetimeScope : LifetimeScope
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private PlayerData playerData;

        protected override void Configure(IContainerBuilder builder)
        {
            if (inputActions == null)
                Debug.LogError("[FrontierLifetimeScope] inputActions is not assigned. " +
                    "Re-run Project Ascension > Setup > Build All Scenes.", this);
            if (playerData == null)
                Debug.LogError("[FrontierLifetimeScope] playerData is not assigned. " +
                    "Re-run Project Ascension > Setup > Build All Scenes.", this);

            builder.RegisterInstance(inputActions);
            builder.RegisterInstance(playerData);

            builder.Register<PlayerSimulation>(Lifetime.Singleton);
            builder.Register<PlayerInputHandler>(Lifetime.Singleton);
            builder.Register<PlayerMovement>(Lifetime.Singleton);
            builder.Register<PlayerCamera>(Lifetime.Singleton);

            builder.RegisterComponentInHierarchy<PlayerController>();
        }
    }
}
