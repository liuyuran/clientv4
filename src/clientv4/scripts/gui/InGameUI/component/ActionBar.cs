using game.scripts.manager.player;
using game.scripts.manager.player.settings;
using game.scripts.utils;
using Godot;

namespace game.scripts.gui.InGameUI.component;

/// <summary>
/// gamepad/keyboard action bar
/// </summary>
public partial class ActionBar: Panel {
    private PlayerSettingsManager.PlayerSettings settings => PlayerSettingsManager.instance.GetSettings();
    private Panel _keyboardActionBar;
    private Panel _gamepadActionBar;

    public override void _Ready() {
        _keyboardActionBar = this.FindNodeByName<Panel>("KeyboardActionBar");
        _gamepadActionBar = this.FindNodeByName<Panel>("GamepadActionBar");
        UpdateActionBar();
    }

    private void UpdateActionBar() {
        _keyboardActionBar.Visible = settings.ActionBar.Mode == ActionBarMode.Keyboard;
        _gamepadActionBar.Visible = settings.ActionBar.Mode == ActionBarMode.Gamepad;
    }

    public override void _Process(double delta) {
        if (GameStatus.currentStatus != GameStatus.Status.Playing) return;
    }
}