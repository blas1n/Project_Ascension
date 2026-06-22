using ENet;
using ProjectAscension.Contracts.GameMessages;
using ProjectAscension.GameServer.Network;
using ProjectAscension.GameSimulation.Player;

namespace ProjectAscension.GameServer;

public class GameLoop
{
    private const int MovementTickHz = 20;
    private readonly TimeSpan _movementInterval = TimeSpan.FromMilliseconds(1000.0 / MovementTickHz);

    private readonly ENetTransport _transport;
    private readonly PacketHandler _handler;
    private readonly PacketSender _sender;
    private readonly SessionManager _sessions;
    private readonly ZoneInstance _zone;

    private long _tick;

    public GameLoop(ENetTransport transport, PacketHandler handler, PacketSender sender,
        SessionManager sessions, ZoneInstance zone)
    {
        _transport = transport;
        _handler = handler;
        _sender = sender;
        _sessions = sessions;
        _zone = zone;

        _transport.Connected += OnConnect;
        _transport.Disconnected += OnDisconnect;
        _transport.PacketReceived += (peer, data) => _handler.Handle(peer, data);
        _handler.InputReceived += OnInputReceived;
    }

    private void OnConnect(Peer peer)
    {
        var actorId = Guid.NewGuid();
        _sessions.Register(peer, actorId);
        _zone.AddPlayer(actorId);
        Console.WriteLine($"[GameLoop] Peer {peer.ID} connected as {actorId}");
    }

    private void OnDisconnect(Peer peer)
    {
        var actorId = _sessions.GetActorId(peer);
        if (actorId.HasValue) _zone.RemovePlayer(actorId.Value);
        _sessions.Unregister(peer);
        Console.WriteLine($"[GameLoop] Peer {peer.ID} disconnected.");
    }

    private void OnInputReceived(Peer peer, PlayerInputMessage msg)
    {
        var actorId = _sessions.GetActorId(peer);
        if (!actorId.HasValue) return;

        var input = new PlayerInput(msg.MoveX, msg.MoveZ, msg.Jump, msg.Dodge, msg.Attack, msg.Sequence);
        _zone.ApplyInput(actorId.Value, input, deltaTime: (float)_movementInterval.TotalSeconds);
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var lastMovement = DateTime.UtcNow;
        Console.WriteLine("[GameLoop] Running.");

        while (!ct.IsCancellationRequested)
        {
            _transport.Poll();

            var now = DateTime.UtcNow;
            if (now - lastMovement >= _movementInterval)
            {
                BroadcastWorldState();
                lastMovement = now;
                _tick++;
            }

            await Task.Delay(1, ct);
        }
    }

    private void BroadcastWorldState()
    {
        var snapshots = _zone.PlayerStates
            .Select(kv => new EntitySnapshot(
                kv.Key,
                kv.Value.Position.X, kv.Value.Position.Y, kv.Value.Position.Z,
                kv.Value.Velocity.X, kv.Value.Velocity.Y, kv.Value.Velocity.Z))
            .ToArray();

        var message = new WorldStateMessage(_tick, snapshots);

        foreach (var actorId in _sessions.AllActors)
        {
            var peer = _sessions.GetPeer(actorId);
            if (peer.HasValue) _sender.SendWorldState(peer.Value, message);
        }
    }
}
