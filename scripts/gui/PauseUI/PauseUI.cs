using System.Collections.Generic;
using game.scripts.manager;
using Godot;

namespace game.scripts.gui.PauseUI;

/// <summary>
/// a menu that is shown when the game is paused
/// just like FF14, a horizontal menu with buttons to switch between different vertical menus
/// </summary>
public partial class PauseUI : Control {
    private readonly List<Control> _menuGroups = [];
    private ScrollContainer _scroll;
    private short _currentGroupIndex;
    private short _currentFocusButtonIndex;

    public override void _Ready() {
        ProcessMode = ProcessModeEnum.Always;
        _scroll = GetParent<ScrollContainer>();
        GetTree().Paused = true;
        ReloadAllMenu();
    }

    public override void _ExitTree() {
        GetTree().Paused = false;
    }

    public override void _Process(double delta) {
        if (Input.IsActionJustPressed("ui_scroll_up")) {
            if (_currentGroupIndex >= 0 && _currentGroupIndex < _menuGroups.Count) {
                var targetIndex = _currentFocusButtonIndex - 1;
                if (targetIndex >= 0) {
                    SwitchMenuGroupFocusButton((short)targetIndex);
                }
            }
        } else if (Input.IsActionJustPressed("ui_scroll_down")) {
            if (_currentGroupIndex >= 0 && _currentGroupIndex < _menuGroups.Count) {
                var currentGroup = _menuGroups[_currentGroupIndex];
                var targetIndex = _currentFocusButtonIndex + 1;
                if (targetIndex < currentGroup.GetChildCount()) {
                    SwitchMenuGroupFocusButton((short)targetIndex);
                }
            }
        }

        if (Input.IsActionJustPressed("ui_left")) {
            SwitchMenuGroup((short)Mathf.Max(0, _currentGroupIndex - 1));
        } else if (Input.IsActionJustPressed("ui_right")) {
            SwitchMenuGroup((short)Mathf.Min(_menuGroups.Count - 1, _currentGroupIndex + 1));
        } else if (Input.IsActionJustPressed("ui_up")) {
            var upIndex = (short)Mathf.Max(0, _currentFocusButtonIndex - 1);
            SwitchMenuGroupFocusButton(upIndex);
        } else if (Input.IsActionJustPressed("ui_down")) {
            var currentGroup = _menuGroups[_currentGroupIndex];
            var downIndex = (short)Mathf.Min(currentGroup.GetChildCount() - 1, _currentFocusButtonIndex + 1);
            SwitchMenuGroupFocusButton(downIndex);
        }

        var leftX = Input.GetJoyAxis(0, JoyAxis.LeftX);
        var leftY = Input.GetJoyAxis(0, JoyAxis.LeftY);

        switch (leftX) {
            case <= -0.5f:
                SwitchMenuGroup((short)Mathf.Max(0, _currentGroupIndex - 1));
                break;
            case >= 0.5f:
                SwitchMenuGroup((short)Mathf.Min(_menuGroups.Count - 1, _currentGroupIndex + 1));
                break;
        }

        switch (leftY) {
            case <= -0.5f: {
                var upIndex = (short)Mathf.Max(0, _currentFocusButtonIndex - 1);
                SwitchMenuGroupFocusButton(upIndex);
                break;
            }
            case >= 0.5f: {
                var currentGroup = _menuGroups[_currentGroupIndex];
                var downIndex = (short)Mathf.Min(currentGroup.GetChildCount() - 1, _currentFocusButtonIndex + 1);
                SwitchMenuGroupFocusButton(downIndex);
                break;
            }
        }
    }

    private void ReloadAllMenu() {
        foreach (var group in _menuGroups) {
            RemoveChild(group);
        }

        _menuGroups.Clear();
        var menu = MenuManager.instance.GetMenuGroups();
        if (menu == null || menu.Length == 0) {
            GD.PrintErr("No menu groups found.");
            return;
        }

        for (short index = 0; index < menu.Length; index++) {
            var group = menu[index];
            LoadMenuGroup(group, index);
        }
    }

    private void LoadMenuGroup(MenuManager.MenuItem[] menuItems, short index) {
        var groupContainer = new VBoxContainer();
        AddChild(groupContainer);
        groupContainer.Name = "menuGroup-" + menuItems[0].Id;
        groupContainer.CustomMinimumSize = new Vector2(200, 100);
        _menuGroups.Add(groupContainer);
        foreach (var item in menuItems) {
            LoadMenuItem(groupContainer, item);
        }

        groupContainer.MouseEntered += () => SwitchMenuGroup(index);
    }

    private void LoadMenuItem(Control parent, MenuManager.MenuItem menuItems) {
        var menuButton = new Button();
        parent.AddChild(menuButton);
        menuButton.Name = "menuButton-" + menuItems.Id;
        menuButton.Text = menuItems.Name;
        menuButton.CustomMinimumSize = new Vector2(150, 100);
        menuButton.Pressed += menuItems.Action;
    }

    /// <summary>
    /// switch to a menu group by index
    /// can be active by mouse hover or L-left/L-right in gamepad
    /// </summary>
    /// <param name="index">group index</param>
    private void SwitchMenuGroup(short index) {
        if (_currentGroupIndex == index) return;
        SwitchMenuGroupFocusButton(0);
        _currentGroupIndex = index;

        var targetScroll = 0;

        for (short i = 0; i < _menuGroups.Count; i++) {
            var group = _menuGroups[i];
            targetScroll += (int)group.GetRect().Size.X;
        }

        _scroll.ScrollHorizontal = targetScroll;
        _currentFocusButtonIndex = 0;
        SwitchMenuGroupFocusButton(_currentFocusButtonIndex);
    }

    /// <summary>
    /// change the focus button of the current menu group
    /// the focus button will show in the center of the menu vertical line
    /// </summary>
    /// <param name="index">menu button index</param>
    private void SwitchMenuGroupFocusButton(short index) {
        _currentFocusButtonIndex = index;
        if (_currentGroupIndex < 0 || _currentGroupIndex >= _menuGroups.Count) return;
        var currentGroup = _menuGroups[_currentGroupIndex];
        if (index < 0 || index >= currentGroup.GetChildCount()) return;
        currentGroup.Position = new Vector2(currentGroup.Position.X, -index * 100);
    }
}