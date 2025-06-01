using System.Linq;
using game.scripts.utils;
using Godot;

namespace game.scripts.server;

/// <summary>
/// 用于游戏初始化，必须挂载于游戏场景内
/// </summary>
public partial class MultiPlayerSupportNode: Node {
    private BuildInServer _server = new();
    private bool _initialized;

    public override void _Process(double delta) {
        if (!_initialized) {
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
        }
    }

    public override void _ExitTree() {
        RemoveChild(_server);
        _server.QueueFree();
    }
}