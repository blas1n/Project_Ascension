using ProjectAscension.GameServer;
using ProjectAscension.GameServer.Network;

const ushort Port = 7777;
const string ApiBaseUrl = "http://localhost:5000";

using var transport = new ENetTransport();
var handler = new PacketHandler();
var sender = new PacketSender(transport);
var sessions = new SessionManager();
var zone = new ZoneInstance();
var reporter = new ApiReporter(ApiBaseUrl);
var loop = new GameLoop(transport, handler, sender, sessions, zone);

transport.Start(Port);
Console.WriteLine($"[GameServer] Listening on port {Port}");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

await loop.RunAsync(cts.Token);
Console.WriteLine("[GameServer] Stopped.");
