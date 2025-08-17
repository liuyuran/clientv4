using System.Collections.Generic;
using game.scripts.manager.player;
using game.scripts.manager.player.settings;
using game.scripts.utils;
using Godot;

namespace game.scripts.gui.InGameUI.component;

/// <summary>
/// gamepad/keyboard action bar
/// </summary>
public partial class ActionBar: Panel {
    [Export] private PackedScene _actionBarItem;
    private PlayerSettingsManager.PlayerSettings settings => PlayerSettingsManager.instance.GetSettings();
    private Panel _keyboardActionBar;
    private Panel _gamepadActionBar;
    private ActionArea _activeArea = ActionArea.None;

    public override void _Ready() {
        _keyboardActionBar = this.FindNodeByName<Panel>("KeyboardActionBar");
        _gamepadActionBar = this.FindNodeByName<Panel>("GamepadActionBar");
        UpdateActionBar();
        for (var i = 0; i < 32; i++) {
            var area = (ActionArea) Mathf.FloorToInt(i / 8);
            var btn = _gamepadActionBar.FindNodeByName<Button>("GamepadBtn" + (i + 1));
            var index = i;
            btn.Pressed += () => {
                OnGamepadOnActiveArea(area);
                OnGamePadButtonPressed(index % 8);
            };
        }
    }

    private void UpdateActionBar() {
        _keyboardActionBar.Visible = settings.ActionBar.Mode == ActionBarMode.Keyboard;
        _gamepadActionBar.Visible = settings.ActionBar.Mode == ActionBarMode.Gamepad;
        UpdateActionBarGamepad();
        UpdateActionBarKeyboard();
    }

    private void UpdateActionBarGamepad() {
        for (var i = 0; i < 32; i++) {
            var area = (ActionArea) Mathf.FloorToInt(i / 8);
            var btn = _gamepadActionBar.FindNodeByName<Button>("GamepadBtn" + (i + 1));
            // TODO draw
        }
    }
    
    private void UpdateActionBarKeyboard() {}

    public override void _Process(double delta) {
        if (GameStatus.currentStatus != GameStatus.Status.Playing) return;
    }

    private void OnGamepadOnActiveArea(ActionArea area) {
        _activeArea = area;
    }

    private void OnGamePadButtonPressed(int button) {
        if (_activeArea == ActionArea.None) return;
        var offset = (int) _activeArea * 8;
        var action = button + offset;
        var actions = settings.ActionBar.GamePadItems;
        if (action < 0 || action >= actions.Count) return;
        OnActiveAction(ref actions, action);
    }

    private void OnActiveAction(ref List<ActionItem> item, int index) {
        //
    }

    private enum ActionArea {
        None = -1,
        TopLeft = 0,
        TopRight = 1,
        BottomLeft = 2,
        BottomRight = 3
    }
}