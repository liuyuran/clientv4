using System.Collections.Generic;
using game.scripts.config;
using game.scripts.manager;
using Godot;

namespace game.scripts.gui.PauseUI;

/// <summary>
/// a menu that is shown when the game is paused
/// just like FF14, a horizontal menu with buttons to switch between different vertical menus
/// </summary>
public partial class PauseUI : Control {
    private readonly List<Control> _menuGroups = [];
    private Control _parent;
    private short _currentGroupIndex = -1;
    private short _currentFocusButtonIndex = -1;
    private const int ButtonWidth = 150;
    private const int ButtonHeight = 20;
    private const int ButtonSpacing = 5;
    private ulong _lastSwitchTime;

    public override void _Ready() {
        ProcessMode = ProcessModeEnum.Always;
        _parent = GetParent<Control>();
        GetTree().Paused = true;
        ReloadAllMenu();
        GetTree().Root.SizeChanged += OnRootOnSizeChanged;
    }

    private void OnRootOnSizeChanged() {
        SwitchMenuGroup(_currentGroupIndex, true);
        SwitchMenuGroupFocusButton(_currentFocusButtonIndex, true);
    }

    public override void _ExitTree() {
        GetTree().Paused = false;
    }

    public override void _Process(double delta) {
        if (InputManager.instance.IsKeyPressed(InputKey.UIScrollUp)) {
            if (_currentGroupIndex >= 0 && _currentGroupIndex < _menuGroups.Count) {
                var targetIndex = _currentFocusButtonIndex - 1;
                if (targetIndex >= 0) {
                    SwitchMenuGroupFocusButton((short)targetIndex);
                }
            }
        } else if (InputManager.instance.IsKeyPressed(InputKey.UIScrollDown)) {
            if (_currentGroupIndex >= 0 && _currentGroupIndex < _menuGroups.Count) {
                var currentGroup = _menuGroups[_currentGroupIndex];
                var targetIndex = _currentFocusButtonIndex + 1;
                if (targetIndex < currentGroup.GetChildCount()) {
                    SwitchMenuGroupFocusButton((short)targetIndex);
                }
            }
        }

        if (InputManager.instance.IsKeyPressed(InputKey.UILeft)) {
            SwitchMenuGroup((short)Mathf.Max(0, _currentGroupIndex - 1));
        } else if (InputManager.instance.IsKeyPressed(InputKey.UIRight)) {
            SwitchMenuGroup((short)Mathf.Min(_menuGroups.Count - 1, _currentGroupIndex + 1));
        } else if (InputManager.instance.IsKeyPressed(InputKey.UIUp)) {
            var upIndex = (short)Mathf.Max(0, _currentFocusButtonIndex - 1);
            SwitchMenuGroupFocusButton(upIndex);
        } else if (InputManager.instance.IsKeyPressed(InputKey.UIDown)) {
            var currentGroup = _menuGroups[_currentGroupIndex];
            var downIndex = (short)Mathf.Min(currentGroup.GetChildCount() - 1, _currentFocusButtonIndex + 1);
            SwitchMenuGroupFocusButton(downIndex);
        }

        var (leftX, leftY) = InputManager.instance.GetRightStickVector();

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
            LoadMenuGroup(group);
        }

        SwitchMenuGroup(0);
        SwitchMenuGroupFocusButton(0);
    }

    private void LoadMenuGroup(MenuManager.MenuItem[] menuItems) {
        var groupContainer = new VBoxContainer();
        AddChild(groupContainer);
        groupContainer.Name = "menuGroup-" + menuItems[0].Id;
        groupContainer.CustomMinimumSize = new Vector2(0, ButtonHeight);
        _menuGroups.Add(groupContainer);
        foreach (var item in menuItems) {
            LoadMenuItem(groupContainer, item);
        }
        var margin = new VBoxContainer();
        AddChild(margin);
        margin.CustomMinimumSize = new Vector2(ButtonSpacing, ButtonHeight);
    }

    private void LoadMenuItem(Control parent, MenuManager.MenuItem menuItems) {
        var menuButton = new Button();
        parent.AddChild(menuButton);
        menuButton.Name = "menuButton-" + menuItems.Id;
        menuButton.Text = menuItems.Name;
        menuButton.CustomMinimumSize = new Vector2(ButtonWidth, ButtonHeight);
        menuButton.Pressed += menuItems.Action;
    }

    /// <summary>
    /// switch to a menu group by index
    /// can be active by mouse hover or L-left/L-right in gamepad
    /// </summary>
    /// <param name="index">group index</param>
    /// <param name="ignoreCooldown">ignore input cool down</param>
    private void SwitchMenuGroup(short index, bool ignoreCooldown = false) {
        if (Time.GetTicksMsec() - _lastSwitchTime < 500 && !ignoreCooldown) return;
        _lastSwitchTime = Time.GetTicksMsec();
        if (_currentGroupIndex == index) return;
        _currentGroupIndex = index;
        var totalWidth = ButtonWidth * _menuGroups.Count + ButtonSpacing * (_menuGroups.Count - 1);
        Size = new Vector2(totalWidth, ButtonHeight);
        var initX = Mathf.Max((_parent.Size.X - totalWidth) / 2, _parent.Size.X * 0.01);
        var minX = initX - index * (ButtonWidth + ButtonSpacing);
        var target = Vector2.Zero;
        target.X = Mathf.Clamp(initX - index * (ButtonWidth + totalWidth), minX, initX);
        target.Y = (_parent.Size.Y - ButtonHeight) / 2;
        Position = target;
        SwitchMenuGroupFocusButton(0, true);
    }

    /// <summary>
    /// change the focus button of the current menu group
    /// the focus button will show in the center of the menu vertical line
    /// </summary>
    /// <param name="index">menu button index</param>
    /// <param name="ignoreCooldown">ignore input cool down</param>
    private void SwitchMenuGroupFocusButton(short index, bool ignoreCooldown = false) {
        if (Time.GetTicksMsec() - _lastSwitchTime < 500 && !ignoreCooldown) return;
        _lastSwitchTime = Time.GetTicksMsec();
        if (_currentFocusButtonIndex == index) return;
        _currentFocusButtonIndex = index;
        if (_currentGroupIndex < 0 || _currentGroupIndex >= _menuGroups.Count) return;
        var currentGroup = _menuGroups[_currentGroupIndex];
        if (index < 0 || index >= currentGroup.GetChildCount()) return;
        currentGroup.Position = new Vector2(currentGroup.Position.X, -index * (ButtonHeight + 15));
    }
}