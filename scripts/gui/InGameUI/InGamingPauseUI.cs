using game.scripts.config;
using Godot;

namespace game.scripts.gui.InGameUI;

/// <summary>
/// in game UI - pause part
/// </summary>
public partial class InGamingUI {
    private ulong _lastPauseTime;
    private Control _pauseUI;
    
    private void TryOpenPauseUI() {
        if (_status.Focus == InGameUIFocus.Pause) {
            return;
        }
        if (InputManager.instance.IsKeyPressed(InputKey.SwitchPause) && _pauseUI == null && Time.GetTicksMsec() - _lastPauseTime > 500) {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            _pauseUI = PauseUI.Instantiate<Control>();
            AddChild(_pauseUI);
            _status.Focus = InGameUIFocus.Pause;
            _lastPauseTime = Time.GetTicksMsec();
        }
    }
	
    private void TryClosePauseUI() {
        if (_status.Focus != InGameUIFocus.Pause) {
            return;
        }
        if (InputManager.instance.IsKeyPressed(InputKey.UICancel) && _pauseUI != null && Time.GetTicksMsec() - _lastPauseTime > 500) {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            RemoveChild(_pauseUI);
            _pauseUI = null;
            _status.Focus = InGameUIFocus.Game;
            _lastPauseTime = Time.GetTicksMsec();
        }
    }
}