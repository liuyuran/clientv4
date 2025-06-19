using game.scripts.config;
using game.scripts.gui.InGameUI.component;
using game.scripts.manager;
using game.scripts.utils;
using Godot;

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
        _debugInfo = _playingUI.GetNode<Panel>("debug");
        RebindScript();
    }

    private void RebindScript() {
        var chatScroll = _playingUI.GetNode<ChatScroll>("chat");
        chatScroll.InGamingUIInstance = this;
        _playingUI.GetNode<Panel>("debug");
    }

    private void ClosePlayingUI() {
        RemoveChild(_playingUI);
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
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    public void BroadcastChatMessage(string message) {
        Rpc(MethodName.ReceiveChatMessage, message);
    }

    /// <summary>
    /// Clients receive chat message.
    /// </summary>
    [Rpc(CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void ReceiveChatMessage(string message) {
        ChatManager.instance.AddMessage(new ChatManager.MessageInfo {
            Message = message
        });
    }
}