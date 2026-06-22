using ENet;

namespace ProjectAscension.GameServer.Network;

public sealed class ENetTransport : IDisposable
{
    private Host _host = null!;

    public event Action<Peer>? Connected;
    public event Action<Peer>? Disconnected;
    public event Action<Peer, byte[]>? PacketReceived;

    public void Start(ushort port, int maxClients = 100)
    {
        Library.Initialize();
        _host = new Host();
        var address = new Address { Port = port };
        _host.Create(address, maxClients);
    }

    public void Poll()
    {
        while (_host.Service(0, out var netEvent) > 0)
        {
            switch (netEvent.Type)
            {
                case EventType.Connect:
                    Connected?.Invoke(netEvent.Peer);
                    break;
                case EventType.Receive:
                    var data = new byte[netEvent.Packet.Length];
                    netEvent.Packet.CopyTo(data);
                    netEvent.Packet.Dispose();
                    PacketReceived?.Invoke(netEvent.Peer, data);
                    break;
                case EventType.Disconnect:
                    Disconnected?.Invoke(netEvent.Peer);
                    break;
            }
        }
    }

    public void Send(Peer peer, byte[] data, byte channelId = 0, PacketFlags flags = PacketFlags.Reliable)
    {
        var packet = default(Packet);
        packet.Create(data, flags);
        peer.Send(channelId, ref packet);
    }

    public void Dispose()
    {
        _host?.Dispose();
        Library.Deinitialize();
    }
}
