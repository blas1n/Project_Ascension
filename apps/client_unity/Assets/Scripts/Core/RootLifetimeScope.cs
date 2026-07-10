using VContainer;
using VContainer.Unity;

namespace ProjectAscension.Core
{
    /// <summary>
    /// VContainer root scope. Lives in the Bootstrap scene and persists for the
    /// lifetime of the app. Cross-scene services are registered here.
    /// </summary>
    public sealed class RootLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // Registered in later phases (architecture hooks only for now):
            //   builder.Register<ApiClient>(Lifetime.Singleton);
            //   builder.Register<ContractService>(Lifetime.Singleton);
            //   builder.Register<CharacterStateService>(Lifetime.Singleton);
        }
    }
}
