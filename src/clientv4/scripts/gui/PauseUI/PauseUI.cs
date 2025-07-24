using System.Collections.Generic;
using game.scripts.config;
using game.scripts.manager;
using game.scripts.manager.menu;
using Godot;

namespace game.scripts.gui.PauseUI;

/// <summary>
/// a menu that is shown when the game is paused
/// just like FF14, a horizontal menu with buttons to switch between different vertical menus
/// </summary>
public partial class PauseUI : Control {
    private readonly List<Control> _menuGroups = [];
    private MenuManager.MenuItem[][] currentData;
    private Control _menuTip;
    private RichTextLabel _menuTipText;
    private Control _parent;
    private short _currentGroupIndex = -1;
    private short _currentFocusButtonIndex = -1;
    private const int ButtonWidth = 150;
    private const int ButtonHeight = 20;
    private const int ButtonSpacing = 5;
    private ulong _lastSwitchTime;
    private ulong _lastConfirmTime;

    public override void _Ready() {
        ProcessMode = ProcessModeEnum.Always;
        _parent = GetParent<Control>();
        _menuTip = _parent.GetNode<Control>("Description");
        _menuTipText = _menuTip.GetNode<RichTextLabel>("Text");
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

        if (InputManager.instance.IsKeyPressed(InputKey.UIConfirm) && Time.GetTicksMsec() - _lastConfirmTime > 500) {
            _lastConfirmTime = Time.GetTicksMsec();
            if (_currentGroupIndex < 0 || _currentGroupIndex >= currentData.Length) {
                GD.PrintErr("Current group index is out of range.");
                return;
            }
            var currentGroup = currentData[_currentGroupIndex];
            if (_currentFocusButtonIndex < 0 || _currentFocusButtonIndex >= currentGroup.Length) {
                GD.PrintErr("Current focus button index is out of range.");
                return;
            }
            var currentButton = currentGroup[_currentFocusButtonIndex];
            if (currentButton.Action != null) {
                currentButton.Action.Invoke();
            } else {
                GD.PrintErr("Current button action is null.");
            }
        } else if (InputManager.instance.IsKeyPressed(InputKey.UICancel)) {
            //
        }
    }

    private void ReloadAllMenu() {
        foreach (var group in _menuGroups) {
            group.QueueFree();
        }

        _menuGroups.Clear();
        var menu = MenuManager.instance.GetMenuGroups();
        if (menu == null || menu.Length == 0) {
            GD.PrintErr("No menu groups found.");
            return;
        }
        currentData = menu;

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
        if (Time.GetTicksMsec() - _lastSwitchTime < 200 && !ignoreCooldown) return;
        _lastSwitchTime = Time.GetTicksMsec();
        if (_currentGroupIndex == index) return;
        if (_currentGroupIndex > 0) {
            _menuGroups[_currentGroupIndex].Position = new Vector2(_menuGroups[_currentGroupIndex].Position.X, 0);
        }
        _currentGroupIndex = index;
        var totalWidth = ButtonWidth * _menuGroups.Count + ButtonSpacing * (_menuGroups.Count - 1);
        Size = new Vector2(totalWidth, ButtonHeight);
        var initX = Mathf.Max((_parent.Size.X - totalWidth) / 2, _parent.Size.X * 0.01);
        var minX = Mathf.Min(initX, _parent.Size.X * 0.99 - totalWidth);
        var target = Vector2.Zero;
        target.X = Mathf.Clamp(initX - index * (ButtonWidth + totalWidth), minX, initX);
        target.Y = (_parent.Size.Y - ButtonHeight) / 2;
        Position = target;
        SwitchMenuGroupFocusButton(0, true);
        var tipWidth = _parent.Size.X * 0.8;
        _menuTip.Size = new Vector2(tipWidth, ButtonHeight);
        _menuTipText.Size = _menuTip.Size;
        _menuTip.Position = new Vector2(
            _parent.Size.X * 0.3,
            (_parent.Size.Y - ButtonHeight) / 2 - ButtonHeight - 15
        );
        _menuTipText.Text = currentData[_currentGroupIndex][_currentFocusButtonIndex].Description;
    }

    /// <summary>
    /// change the focus button of the current menu group
    /// the focus button will show in the center of the menu vertical line
    /// </summary>
    /// <param name="index">menu button index</param>
    /// <param name="ignoreCooldown">ignore input cool down</param>
    private void SwitchMenuGroupFocusButton(short index, bool ignoreCooldown = false) {
        if (Time.GetTicksMsec() - _lastSwitchTime < 200 && !ignoreCooldown) return;
        _lastSwitchTime = Time.GetTicksMsec();
        if (_currentFocusButtonIndex == index) return;
        _currentFocusButtonIndex = index;
        if (_currentGroupIndex < 0 || _currentGroupIndex >= _menuGroups.Count) return;
        var currentGroup = _menuGroups[_currentGroupIndex];
        if (index < 0 || index >= currentGroup.GetChildCount()) return;
        currentGroup.Position = new Vector2(currentGroup.Position.X, -index * (ButtonHeight + 15));
        _menuTipText.Text = currentData[_currentGroupIndex][_currentFocusButtonIndex].Description;
    }
}