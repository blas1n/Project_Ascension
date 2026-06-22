using ENet;
using MessagePack;
using ProjectAscension.Contracts.GameMessages;

namespace ProjectAscension.GameServer.Network;

public class PacketSender
{
    private readonly ENetTransport _transport;
    public PacketSender(ENetTransport transport) => _transport = transport;

    public void SendWorldState(Peer peer, WorldStateMessage message)
    {
        var data = MessagePackSerializer.Serialize(message);
        _transport.Send(peer, data, channelId: 0, PacketFlags.Unsequenced);
    }

    public void SendGameEvent(Peer peer, GameEventMessage message)
    {
        var data = MessagePackSerializer.Serialize(message);
        _transport.Send(peer, data, channelId: 1, PacketFlags.Reliable);
    }
}
