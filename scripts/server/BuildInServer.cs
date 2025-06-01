using System.Threading;
using game.scripts.manager;
using game.scripts.utils;
using Godot;

namespace game.scripts.server;

/// <summary>
/// 内置服务器/客户端，需要在创建的时候传入连接模式，并将其加入Node树。
/// 但此节点*永远*不应硬性挂载到任何场景中。
/// </summary>
public partial class BuildInServer : Node {
    private const int Port = 7000;
    private const string DefaultServerIp = "127.0.0.1";
    private const int MaxConnections = 20;

    public override void _Ready() {
        Multiplayer.PeerConnected += OnPlayerConnected;
        Multiplayer.PeerDisconnected += OnPlayerDisconnected;
        Multiplayer.ConnectedToServer += OnConnectOk;
        Multiplayer.ConnectionFailed += OnConnectionFail;
        Multiplayer.ServerDisconnected += OnServerDisconnected;
    }

    private static string GetNickname() {
        return ServerStartupConfig.instance.nickname;
    }

    public Error JoinGame(string address = DefaultServerIp, int port = Port) {
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateClient(address, port);

        if (error != Error.Ok) {
            GD.PrintErr($"Failed to connect to server at {address}:{port}. Error: {error}");
            return error;
        }

        Multiplayer.MultiplayerPeer = peer;
        return Error.Ok;
    }

    private Thread _pingThread;
    private volatile bool _shouldPingThreadRun = true;
    
    public Error CreateGame() {
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateServer(Port, MaxConnections);
    
        if (error != Error.Ok) {
            GD.PrintErr($"Failed to create server on port {Port}. Error: {error}");
            return error;
        }
    
        Multiplayer.MultiplayerPeer = peer;
        PlayerManager.instance.RegisterPlayer(peer.GetUniqueId(), GetNickname());
        
        _pingThread = new Thread(PingMeasurementLoop);
        _pingThread.Start();
        
        return Error.Ok;
    }

    private void OnPlayerConnected(long id) {
        GD.Print($"Player connected: {id}");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void LoginAsPlayer(string nickname) {
        if (PlatformUtil.isNetworkMaster) {
            var peerId = Multiplayer.GetRemoteSenderId();
            PlayerManager.instance.RegisterPlayer(peerId, nickname);
        }
    }

    private void OnPlayerDisconnected(long id) {
        PlayerManager.instance.RemovePlayer(id);
    }

    private void OnConnectOk() {
        GD.Print("Connected to server successfully.");
        Rpc(MethodName.LoginAsPlayer, GetNickname());
    }

    private void OnConnectionFail() {
        Multiplayer.MultiplayerPeer = null;
        GD.PrintErr("Connection failed. Please check the server address and port.");
    }

    private void OnServerDisconnected() {
        Multiplayer.MultiplayerPeer = null;
        GD.Print("Server disconnected. You can try to reconnect or create a new game.");
    }
    
    public override void _ExitTree() {
        _shouldPingThreadRun = false;
        _pingThread?.Join();
        base._ExitTree();
    }
    
    private void PingMeasurementLoop() {
        while (_shouldPingThreadRun) {
            if (PlatformUtil.isNetworkMaster) {
                foreach (var player in PlayerManager.instance.GetAllPlayers()) {
                    CallDeferred(MethodName.SendPingRequest, player.peerId);
                }
            }
            Thread.Sleep(3000);
        }
    }
    
    [Rpc]
    private void SendPingRequest(long peerId) {
        if (!PlatformUtil.isNetworkMaster) return;
        var timestamp = Time.GetTicksMsec();
        if (peerId == 1) {
            ReceivePingResponse(timestamp);
            return;
        }
        RpcId(peerId, MethodName.RespondToPing, timestamp);
    }
    
    [Rpc]
    private void RespondToPing(ulong serverTimestamp) {
        var senderId = Multiplayer.GetRemoteSenderId();
        RpcId(senderId, MethodName.ReceivePingResponse, serverTimestamp);
    }
    
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void ReceivePingResponse(ulong serverTimestamp) {
        if (!PlatformUtil.isNetworkMaster) return;
        var ping = (uint)(Time.GetTicksMsec() - serverTimestamp);
        var peerId = Multiplayer.GetRemoteSenderId();
        PlayerManager.instance.UpdatePlayerPing(peerId, ping);
    }
}