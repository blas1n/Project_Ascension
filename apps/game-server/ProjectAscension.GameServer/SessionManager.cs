using ENet;

namespace ProjectAscension.GameServer;

public class SessionManager
{
    private readonly Dictionary<uint, Guid> _peerToActor = new();
    private readonly Dictionary<Guid, Peer> _actorToPeer = new();

    public void Register(Peer peer, Guid actorId)
    {
        _peerToActor[peer.ID] = actorId;
        _actorToPeer[actorId] = peer;
    }

    public void Unregister(Peer peer)
    {
        if (_peerToActor.TryGetValue(peer.ID, out var actorId))
        {
            _peerToActor.Remove(peer.ID);
            _actorToPeer.Remove(actorId);
        }
    }

    public Guid? GetActorId(Peer peer)
        => _peerToActor.TryGetValue(peer.ID, out var id) ? id : null;

    public Peer? GetPeer(Guid actorId)
        => _actorToPeer.TryGetValue(actorId, out var peer) ? peer : null;

    public IEnumerable<Guid> AllActors => _actorToPeer.Keys;
}
