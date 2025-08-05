using game.scripts.config;
using game.scripts.gui.InGameUI.component;
using game.scripts.manager;
using game.scripts.manager.chat;
using game.scripts.manager.player;
using game.scripts.utils;
using Godot;
using ModLoader.chat;

namespace game.scripts.gui.InGameUI;

/// <summary>
/// The controller of UI shown while game running, such as chat, debug info, player list, etc.
/// </summary>
public partial class InGamingUI {
    private Control _playingUI;
    private Panel _debugInfo;
    
    private bool _showDebugLogKeyReleased = true;

    private void OpenPlayingUI() {
        if (_playingUI != null) {
            GD.PrintErr("Playing UI is already open.");
            return;
        }
        _playingUI = PlayingUI.Instantiate<Control>();
        if (_playingUI == null) {
            GD.PrintErr("Failed to instantiate Playing UI.");
            return;
        }
        AddChild(_playingUI);
        _playingUI.Name = "PlayingUI";
        _debugInfo = _playingUI.FindNodeByName<Panel>("debug");
        RebindScript();
    }

    private void RebindScript() {
        var chatScroll = _playingUI.FindNodeByName<ChatScroll>("chat");
        chatScroll.InGamingUIInstance = this;
        _playingUI.FindNodeByName<Panel>("debug");
    }

    private void ClosePlayingUI() {
        _playingUI.QueueFree();
        _playingUI = null;
    }
    
    private void UpdatePlayingUI(double delta) {
        if (_playingUI == null) return;
        /*if (InputManager.instance.IsKeyPressed(InputKey.SwitchDebugInfo) && _showDebugLogKeyReleased) {
            _config.ShowDebugInfo = !_config.ShowDebugInfo;
        } else {
            _showDebugLogKeyReleased = true;
        }
        
        _debugInfo.Visible = _config.ShowDebugInfo;*/
    }

    private void HandleInputOnPlayerUI(InputEvent @event) {
        if (_playingUI == null) return;
        if (@event is InputEventKey eventKey && eventKey.IsPressed() && eventKey.Keycode == Key.Tab) {
            if (PlatformUtil.isNetworkMaster) {
                var (players, ping) = GetPlayerList();
                ShowPlayerList(players, ping);
            } else {
                Rpc(MethodName.FetchPlayerList);
            }
        }
    }

    /* well, because that all panel is prefab now, sometimes they may not be in the node tree, so rpc method must be place in here */
    
    /// <summary>
    /// Server broadcast chat message.
    /// run on server.
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    public void BroadcastChatMessage(ulong timestamp, string message) {
        var parseResult = CommandManager.instance.TryParseCommand(message);
        if (parseResult == null) {
            Rpc(MethodName.ReceiveChatMessage, timestamp, message);
        } else {
            var peerId = Multiplayer.GetRemoteSenderId();
            var playerId = PlayerManager.instance.GetPlayerByPeerId(peerId).playerId;
            CommandManager.instance.ExecuteCommand(playerId, parseResult.Value.commandName, parseResult.Value.args);
        }
    }
    
    /// <summary>
    /// run on a client.
    /// </summary>
    [Rpc(CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    public void SendChatMessageToPlayer(ulong timestamp, string message) {
        var parseResult = CommandManager.instance.TryParseCommand(message);
        if (parseResult == null) {
            Rpc(MethodName.ReceiveChatMessage, timestamp, message);
        } else {
            var peerId = Multiplayer.GetRemoteSenderId();
            var playerId = PlayerManager.instance.GetPlayerByPeerId(peerId).playerId;
            CommandManager.instance.ExecuteCommand(playerId, parseResult.Value.commandName, parseResult.Value.args);
        }
    }

    /// <summary>
    /// Clients receive chat message.
    /// </summary>
    [Rpc(CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void ReceiveChatMessage(ulong timestamp, string message) {
        ChatManager.instance.ReceiveMessage(new MessageInfo {
            Timestamp = timestamp,
            Message = message
        });
    }
}