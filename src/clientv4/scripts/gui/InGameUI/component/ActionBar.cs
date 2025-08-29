using System;
using System.Collections.Generic;
using game.scripts.manager.item;
using game.scripts.manager.player;
using game.scripts.manager.player.settings;
using game.scripts.utils;
using Godot;
using Microsoft.Extensions.Logging;
using ModLoader.logger;

namespace game.scripts.gui.InGameUI.component;

/// <summary>
/// gamepad/keyboard action bar
/// </summary>
public partial class ActionBar : Control {
    private readonly ILogger _logger = LogManager.GetLogger<ActionBar>();
    [Export] public PackedScene ActionItem;
    private PlayerSettingsManager.PlayerSettings settings => PlayerSettingsManager.instance.GetSettings();
    private Control _keyboardActionBar;
    private Control _gamepadActionBar;
    private ActionArea _activeArea = ActionArea.None;

    public override void _Ready() {
        _keyboardActionBar = this.FindNodeByName<Control>("KeyboardActionBar");
        _gamepadActionBar = this.FindNodeByName<Control>("GamepadActionBar");
        UpdateActionBar();
    }

    /// <summary>
    /// refresh action bar status
    /// </summary>
    private void UpdateActionBar() {
        _keyboardActionBar.Visible = settings.ActionBar.Mode == ActionBarMode.Keyboard;
        _gamepadActionBar.Visible = settings.ActionBar.Mode == ActionBarMode.Gamepad;
        UpdateActionBarGamepad();
        UpdateActionBarKeyboard();
    }

    /// <summary>
    /// Fill action to gamepad mod action bar
    /// </summary>
    private void UpdateActionBarGamepad() {
        var lltGroup = _gamepadActionBar.FindNodeByName<Control>("LLT");
        var rrtGroup = _gamepadActionBar.FindNodeByName<Control>("RRT");
        var lbGroup = _gamepadActionBar.FindNodeByName<Control>("LB");
        var rbGroup = _gamepadActionBar.FindNodeByName<Control>("RB");
        var config = PlayerSettingsManager.instance.GetSettings().ActionBar.GamePadItems;
        for (var i = 0; i < 32; i++) {
            if (config.Count <= i) break;
            var configItem = config[i];
            var group = i switch {
                < 8 => lltGroup,
                < 16 => rrtGroup,
                < 24 => lbGroup,
                < 32 => rbGroup,
                _ => throw new ArgumentOutOfRangeException()
            };
            var trulyIndex = i switch {
                < 8 => i,
                < 16 => i - 8,
                < 24 => i - 16,
                < 32 => i - 24,
                _ => throw new ArgumentOutOfRangeException()
            };
            var btn = trulyIndex switch {
                0 => group.FindNodeByName<ActionBarItem>("LLButton"),
                1 => group.FindNodeByName<ActionBarItem>("LTButton"),
                2 => group.FindNodeByName<ActionBarItem>("LRButton"),
                3 => group.FindNodeByName<ActionBarItem>("LBButton"),
                4 => group.FindNodeByName<ActionBarItem>("RLButton"),
                5 => group.FindNodeByName<ActionBarItem>("RTButton"),
                6 => group.FindNodeByName<ActionBarItem>("RRButton"),
                7 => group.FindNodeByName<ActionBarItem>("RBButton"),
                _ => null
            };
            btn?.SetItem(ActionBarItemType.GamepadActionBar, configItem, i, 1);
        }
        ActionBarItem.OnItemChanged += RefreshActionBar;
    }

    private void RefreshActionBar(ActionBarItemType barType) {
        switch (barType) {
            case ActionBarItemType.GamepadActionBar: {
                UpdateActionBarGamepad();
                break;
            }
            case ActionBarItemType.KeyboardActionBar: {
                UpdateActionBarKeyboard();
                break;
            }
        }
    }
    
    /// <summary>
    /// Fill action to keyboard mod action bar
    /// </summary>
    private void UpdateActionBarKeyboard() {
        var childCount = (ulong)_keyboardActionBar.GetChildCount();
        var actionBarCount = InventoryManager.instance.GetToolSlotCount(PlayerManager.instance.GetCurrentPlayerId());
        if (childCount == actionBarCount) return;
        var children = _keyboardActionBar.GetChildren();
        // remove if more
        if (childCount > actionBarCount) {
            for (var i = childCount - 1; i >= actionBarCount; i--) {
                _keyboardActionBar.RemoveChild(children[(int)i]);
            }

            return;
        }

        // add if less
        for (var i = (ulong)0; i < actionBarCount - childCount; i++) {
            var instance = ActionItem.Instantiate<Node>();
            _keyboardActionBar.AddChild(instance);
        }
    }

    private ulong _lastLeftPressTime;
    private ulong _lastRightPressTime;
    private bool _leftPressed;
    private bool _rightPressed;
    private bool _leftDoubleClick;
    private bool _rightDoubleClick;
    private readonly bool[] _buttonPressed = new bool[8]; // 跟踪8个按键的状态
    private readonly ulong[] _lastButtonPressTime = new ulong[8]; // 跟踪按键的最后按下时间

    private bool ShouldRepeatButton(int button) {
        // TODO 根据快捷键类型决定是否重复触发
        return true;
    }
    
    /// <summary>
    /// handle input
    /// </summary>
    public override void _Process(double delta) {
        if (GameStatus.currentStatus != GameStatus.Status.Playing) return;

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (settings.ActionBar.Mode) {
            case ActionBarMode.Keyboard: {
                // TODO handle quick slot key
                break;
            }
            case ActionBarMode.Gamepad: {
                var leftAxis = Input.GetJoyAxis(0, JoyAxis.TriggerLeft);
                var rightAxis = Input.GetJoyAxis(0, JoyAxis.TriggerRight);
                var currentTime = PlatformUtil.GetTimestamp();

                switch (leftAxis) {
                    // 处理LT按键
                    case > 0 when !_leftPressed: {
                        // 按下LT
                        _leftPressed = true;
                        if (currentTime - _lastLeftPressTime <= 500 && !_leftDoubleClick) {
                            // 双击
                            _leftDoubleClick = true;
                            OnGamepadOnActiveArea(ActionArea.TopLeft);
                        } else {
                            // 单击
                            _leftDoubleClick = false;
                            OnGamepadOnActiveArea(ActionArea.BottomLeft);
                        }

                        _lastLeftPressTime = currentTime;
                        break;
                    }
                    case <= 0 when _leftPressed:
                        // 松开LT
                        _leftPressed = false;
                        _leftDoubleClick = false;
                        OnGamepadOnActiveArea(ActionArea.None);
                        break;
                }

                switch (rightAxis) {
                    // 处理RT按键
                    case > 0 when !_rightPressed: {
                        // 按下RT
                        _rightPressed = true;
                        if (currentTime - _lastRightPressTime <= 500 && !_rightDoubleClick) {
                            // 双击
                            _rightDoubleClick = true;
                            OnGamepadOnActiveArea(ActionArea.TopRight);
                        } else {
                            // 单击
                            _rightDoubleClick = false;
                            OnGamepadOnActiveArea(ActionArea.BottomRight);
                        }

                        _lastRightPressTime = currentTime;
                        break;
                    }
                    case <= 0 when _rightPressed:
                        // 松开RT
                        _rightPressed = false;
                        _rightDoubleClick = false;
                        OnGamepadOnActiveArea(ActionArea.None);
                        break;
                }
                
                // 手柄按键响应
                if (_activeArea != ActionArea.None) {
                    // 检测按键状态并处理触发
                    var buttonStates = new bool[] {
                        Input.IsJoyButtonPressed(0, JoyButton.DpadLeft),   // 0
                        Input.IsJoyButtonPressed(0, JoyButton.DpadUp),     // 1
                        Input.IsJoyButtonPressed(0, JoyButton.DpadRight),  // 2
                        Input.IsJoyButtonPressed(0, JoyButton.DpadDown),   // 3
                        Input.IsJoyButtonPressed(0, JoyButton.X),          // 4
                        Input.IsJoyButtonPressed(0, JoyButton.Y),          // 5
                        Input.IsJoyButtonPressed(0, JoyButton.B),          // 6
                        Input.IsJoyButtonPressed(0, JoyButton.A)           // 7
                    };
                
                    for (var i = 0; i < buttonStates.Length; i++) {
                        switch (buttonStates[i]) {
                            case true when !_buttonPressed[i]:
                                // 按键刚按下，首次触发
                                _buttonPressed[i] = true;
                                _lastButtonPressTime[i] = currentTime;
                                OnGamePadButtonPressed(i);
                                break;
                            case true when _buttonPressed[i] && ShouldRepeatButton(i): {
                                // 按键持续按下且需要重复触发
                                if (currentTime - _lastButtonPressTime[i] >= 500) {
                                    _lastButtonPressTime[i] = currentTime;
                                    OnGamePadButtonPressed(i);
                                }

                                break;
                            }
                            case false when _buttonPressed[i]:
                                // 按键松开
                                _buttonPressed[i] = false;
                                break;
                        }
                    }
                }

                break;
            }
        }
    }

    private Control _lastActiveControl;

    /// <summary>
    /// scale gampad action bar area when active
    /// </summary>
    private void OnGamepadOnActiveArea(ActionArea area) {
        _activeArea = area;

        // 重置上一个激活控件的缩放
        if (_lastActiveControl != null) {
            _lastActiveControl.Scale = Vector2.One;
            _lastActiveControl = null;
        }

        // 根据区域获取对应的控件
        var targetControl = area switch {
            ActionArea.TopLeft => _gamepadActionBar.FindNodeByName<Control>("LLT"),
            ActionArea.TopRight => _gamepadActionBar.FindNodeByName<Control>("RRT"),
            ActionArea.BottomLeft => _gamepadActionBar.FindNodeByName<Control>("LB"),
            ActionArea.BottomRight => _gamepadActionBar.FindNodeByName<Control>("RB"),
            _ => null
        };

        // 设置新控件的缩放
        if (targetControl == null) return;
        targetControl.PivotOffset = targetControl.Size / 2;
        targetControl.Scale = Vector2.One * 1.2f;
        _lastActiveControl = targetControl;
    }

    /// <summary>
    /// handle action when gamepad button pressed
    /// </summary>
    private void OnGamePadButtonPressed(int button) {
        _logger.Log(LogLevel.Debug, "ActionBar: OnGamePadButtonPressed {button} in area {area}", button, _activeArea);
        if (_activeArea == ActionArea.None) return;
        var offset = (int)_activeArea * 8;
        var action = button + offset;
        var actions = settings.ActionBar.GamePadItems;
        if (action < 0 || action >= actions.Count) return;
        OnActiveAction(ref actions, action);
    }

    /// <summary>
    /// handle action when action item activated
    /// </summary>
    private void OnActiveAction(ref List<ActionItem> item, int index) {
        _logger.LogDebug("ActionBar: OnActiveAction {index}", index);
    }

    private enum ActionArea {
        None = -1,
        TopLeft = 0,
        TopRight = 1,
        BottomLeft = 2,
        BottomRight = 3
    }
}