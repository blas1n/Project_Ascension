using ENet;
using MessagePack;
using ProjectAscension.Contracts.GameMessages;

namespace ProjectAscension.GameServer.Network;

public class PacketHandler
{
    public event Action<Peer, PlayerInputMessage>? InputReceived;

    public void Handle(Peer peer, byte[] data)
    {
        try
        {
            var input = MessagePackSerializer.Deserialize<PlayerInputMessage>(data);
            InputReceived?.Invoke(peer, input);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PacketHandler] Deserialize error: {ex.Message}");
        }
    }
}
