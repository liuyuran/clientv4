using game.scripts.manager;
using game.scripts.utils;
using Godot;
using Godot.Collections;

namespace game.scripts.gui;

public partial class InGamingUI: CanvasLayer {
    private RichTextLabel _fps; 
        
    public override void _Ready() {
        _fps = GetNode<RichTextLabel>("FPS");
    }

    public override void _Process(double delta) {
        if (_fps != null) _fps.Text = $"FPS: {Engine.GetFramesPerSecond()}";
    }

    public override void _Input(InputEvent @event) {
        // press tab and show player list
        if (@event is InputEventKey eventKey && eventKey.IsPressed() && eventKey.Keycode == Key.Tab) {
            if (PlatformUtil.isNetworkMaster) {
                var (players, ping) = GetPlayerList();
                ShowPlayerList(players, ping);
            } else {
                Rpc(MethodName.FetchPlayerList);
            }
        }
    }
    
    private void ShowPlayerList(Array<string> players, Array<uint> ping) {
        // This method is called when the player list is received
        // You can implement the logic to display the player list here
        GD.Print("Player list received and should be displayed.");
    }
    
    private static (Array<string>, Array<uint>) GetPlayerList() {
        var players = new Array<string>();
        var ping = new Array<uint>();
        var playerList = PlayerManager.instance.GetAllPlayers();
        foreach (var player in playerList) {
            players.Add(player.nickname);
            ping.Add(0); // Placeholder for ping, you can implement actual ping calculation
        }
        return (players, ping);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void FetchPlayerList() {
        var (players, ping) = GetPlayerList();
        RpcId(Multiplayer.GetRemoteSenderId(), MethodName.ReceiveAndShowPlayerList, players, ping);
    }

    [Rpc(CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void ReceiveAndShowPlayerList(Array<string> players, Array<uint> ping) {
        ShowPlayerList(players, ping);
    }
}