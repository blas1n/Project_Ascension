using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;

namespace ProjectAscension.GameSimulation.Physics;

public sealed class PhysicsWorld : IDisposable
{
    private readonly BufferPool _bufferPool;
    public Simulation Simulation { get; }

    public PhysicsWorld()
    {
        _bufferPool = new BufferPool();
        Simulation = Simulation.Create(
            _bufferPool,
            new NarrowPhaseCallbacks(),
            new PoseIntegratorCallbacks(new Vector3(0, -20f, 0)),
            new SolveDescription(8, 1));
    }

    public void Step(float deltaTime) => Simulation.Timestep(deltaTime);

    public void Dispose()
    {
        Simulation.Dispose();
        _bufferPool.Clear();
    }
}

internal struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
{
    public void Initialize(Simulation simulation) { }
    public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin) => true;
    public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB) => true;

    public bool ConfigureContactManifold<TManifold>(
        int workerIndex, CollidablePair pair, ref TManifold manifold,
        out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        pairMaterial = new PairMaterialProperties
        {
            FrictionCoefficient = 1f,
            MaximumRecoveryVelocity = 2f,
            SpringSettings = new SpringSettings(30, 1)
        };
        return true;
    }

    public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold) => true;
    public void Dispose() { }
}

internal struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
{
    private Vector3 _gravity;
    private Vector3Wide _gravityDtWide;

    public PoseIntegratorCallbacks(Vector3 gravity) { _gravity = gravity; _gravityDtWide = default; }

    public readonly AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
    public readonly bool AllowSubstepsForUnconstrainedBodies => false;
    public readonly bool IntegrateVelocityForKinematics => false;

    public void Initialize(Simulation simulation) { }

    public void PrepareForIntegration(float dt)
    {
        _gravityDtWide = Vector3Wide.Broadcast(_gravity * dt);
    }

    public void IntegrateVelocity(
        Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation,
        BodyInertiaWide localInertia, Vector<int> integrationMask,
        int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
    {
        velocity.Linear += _gravityDtWide;
    }
}
