using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using game.scripts.utils;
using generated.server;
using Godot;
using Google.FlatBuffers;

namespace game.scripts.server;

/// <summary>
/// 用于游戏初始化，必须挂载于游戏场景内
/// </summary>
public partial class MultiPlayerSupportNode: Node {
    private BuildInServer _server = new();
    private bool _initialized;
    private byte[] _broadcastData = [];

    public override void _Process(double delta) {
        if (_initialized) return;
        _initialized = true;
        GetTree().Root.AddChild(_server);
        if (OS.GetCmdlineArgs().Contains("--client")) {
            ServerStartupConfig.instance.isLocalServer = false;
        }
        if (PlatformUtil.isNetworkMaster) {
            var err = _server.CreateGame();
            if (err == Error.Ok) return;
            GD.PrintErr($"创建服务器失败: {err}");
        } else {
            var err = _server.JoinGame(ServerStartupConfig.instance.serverIp, ServerStartupConfig.instance.serverPort);
            if (err == Error.Ok) return;
            GD.PrintErr($"连接远程服务器失败: {err}");
        }
        Task.Run(async () => {
            while (ServerStartupConfig.instance.openBroadcast && ServerStartupConfig.instance.isLocalServer) {
                await Task.Delay(1000);
                if (_broadcastData.Length == 0) {
                    var builder = new FlatBufferBuilder(1024);
                    var nameOffset = builder.CreateString(ServerStartupConfig.instance.serverName);
                    var descOffset = builder.CreateString(ServerStartupConfig.instance.serverDesc);
                    ServerMeta.StartServerMeta(builder);
                    ServerMeta.AddName(builder, nameOffset);
                    ServerMeta.AddDesc(builder, descOffset);
                    var offset = ServerMeta.EndServerMeta(builder);
                    builder.Finish(offset.Value);
                    _broadcastData = builder.SizedByteArray();
                }
                var udp = new UdpClient(ServerStartupConfig.instance.serverPort);
                udp.EnableBroadcast = true;
                var broadcast = new IPEndPoint(IPAddress.Broadcast, ServerStartupConfig.instance.serverPort);
                await udp.SendAsync(_broadcastData, broadcast);
            }
        });
    }
}