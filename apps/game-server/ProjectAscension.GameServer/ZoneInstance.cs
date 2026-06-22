using ProjectAscension.GameSimulation.Player;

namespace ProjectAscension.GameServer;

public class ZoneInstance
{
    private readonly Dictionary<Guid, PlayerState> _playerStates = new();
    private readonly PlayerSimulation _sim = new();

    public void AddPlayer(Guid actorId)
        => _playerStates[actorId] = new PlayerState(
            System.Numerics.Vector3.Zero,
            System.Numerics.Vector3.Zero,
            IsGrounded: true,
            InputSequence: 0);

    public void RemovePlayer(Guid actorId) => _playerStates.Remove(actorId);

    public void ApplyInput(Guid actorId, PlayerInput input, float deltaTime)
    {
        if (!_playerStates.TryGetValue(actorId, out var state)) return;
        _playerStates[actorId] = _sim.ApplyInput(state, input, deltaTime);
    }

    public IReadOnlyDictionary<Guid, PlayerState> PlayerStates => _playerStates;
}
