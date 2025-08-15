using System.Collections.Generic;
using System.Linq;
using Godot;

namespace game.scripts.config;

/// <summary>
/// Check input mapping and filter input events.
/// </summary>
public class InputManager {
    public static InputManager instance { get; private set; } = new();
    private readonly Dictionary<string, List<Key[]>> _keyBinds = new();
    private readonly Dictionary<string, List<(InputType type, int id, int direction)>> _extendedBinds = new();
    private Vector2 _mouseMotionAccumulator = Vector2.Zero;
    private float _mouseWheelAccumulator;

    // Define input types
    private enum InputType {
        MouseWheel, // Mouse wheel events
        JoystickButton, // Controller buttons
        DPad // Controller D-pad
    }

    // Define D-pad directions
    private enum DPadDirection {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3
    }

    private InputManager() {
        RegistryKeyBind(InputKey.SwitchDebugInfo, Key.F4);
        RegistryKeyBind(InputKey.SwitchPlayerList, Key.Tab);
        RegistryKeyBind(InputKey.MoveForward, Key.W);
        RegistryKeyBind(InputKey.MoveBackward, Key.S);
        RegistryKeyBind(InputKey.MoveLeft, Key.A);
        RegistryKeyBind(InputKey.MoveRight, Key.D);
        RegistryKeyBind(InputKey.Jump, Key.Space);
        RegistryJoystickButtonBind(InputKey.Jump, JoyButton.Y);
        RegistryKeyBind(InputKey.Crouch, Key.Ctrl);
        RegistryJoystickButtonBind(InputKey.Crouch, JoyButton.B);
        RegistryKeyBind(InputKey.Pause, Key.Escape);
        RegistryJoystickButtonBind(InputKey.Pause, JoyButton.Start);
        RegistryDPadBind(InputKey.MoveForward, 0, (int)DPadDirection.Up);
        RegistryDPadBind(InputKey.MoveBackward, 0, (int)DPadDirection.Down);
        RegistryDPadBind(InputKey.MoveLeft, 0, (int)DPadDirection.Left);
        RegistryDPadBind(InputKey.MoveRight, 0, (int)DPadDirection.Right);

        // UI
        RegistryMouseWheelBind(InputKey.UIScrollUp, 1);
        RegistryMouseWheelBind(InputKey.UIScrollDown, -1);
        RegistryKeyBind(InputKey.UILeft, Key.Left);
        RegistryKeyBind(InputKey.UIRight, Key.Right);
        RegistryKeyBind(InputKey.UIUp, Key.Up);
        RegistryKeyBind(InputKey.UIDown, Key.Down);
        RegistryKeyBind(InputKey.UIConfirm, Key.Enter);
        RegistryKeyBind(InputKey.UICancel, Key.Escape);
        RegistryJoystickButtonBind(InputKey.UILeft, JoyButton.DpadLeft);
        RegistryJoystickButtonBind(InputKey.UIRight, JoyButton.DpadRight);
        RegistryJoystickButtonBind(InputKey.UIUp, JoyButton.DpadUp);
        RegistryJoystickButtonBind(InputKey.UIDown, JoyButton.DpadDown);
        RegistryJoystickButtonBind(InputKey.UIConfirm, JoyButton.A);
        RegistryJoystickButtonBind(InputKey.UICancel, JoyButton.B);
    }

    /// <summary>
    /// Add key bind
    /// </summary>
    /// <param name="keyCode">bind key, need to define in const field</param>
    /// <param name="keys">keys need to press</param>
    private void RegistryKeyBind(string keyCode, params Key[] keys) {
        if (string.IsNullOrEmpty(keyCode)) {
            GD.PushError("Key code cannot be null or empty.");
            return;
        }

        if (keys == null || keys.Length == 0) {
            GD.PushError("Keys cannot be null or empty.");
            return;
        }

        if (!_keyBinds.ContainsKey(keyCode)) {
            _keyBinds[keyCode] = new List<Key[]>();
        }

        _keyBinds[keyCode].Add(keys);
    }

    /// <summary>
    /// Add mouse wheel bind
    /// </summary>
    /// <param name="keyCode">bind key, need to define in const field</param>
    /// <param name="direction">1 for up, -1 for down</param>
    private void RegistryMouseWheelBind(string keyCode, int direction) {
        if (string.IsNullOrEmpty(keyCode)) {
            GD.PushError("Key code cannot be null or empty.");
            return;
        }

        RegisterExtendedBind(keyCode, InputType.MouseWheel, 0, direction);
    }

    /// <summary>
    /// Add joystick button bind
    /// </summary>
    /// <param name="keyCode">bind key, need to define in const field</param>
    /// <param name="button">joystick button</param>
    /// <param name="deviceId">joystick device id, defaults to 0</param>
    private void RegistryJoystickButtonBind(string keyCode, JoyButton button, int deviceId = 0) {
        if (string.IsNullOrEmpty(keyCode)) {
            GD.PushError("Key code cannot be null or empty.");
            return;
        }

        RegisterExtendedBind(keyCode, InputType.JoystickButton, deviceId, (int)button);
    }

    /// <summary>
    /// Add D-pad bind
    /// </summary>
    /// <param name="keyCode">bind key, need to define in const field</param>
    /// <param name="deviceId">joystick device id, defaults to 0</param>
    /// <param name="direction">D-pad direction (0=Up, 1=Right, 2=Down, 3=Left)</param>
    private void RegistryDPadBind(string keyCode, int deviceId, int direction) {
        if (string.IsNullOrEmpty(keyCode)) {
            GD.PushError("Key code cannot be null or empty.");
            return;
        }

        if (direction < 0 || direction > 3) {
            GD.PushError("Invalid D-pad direction. Must be 0-3.");
            return;
        }

        RegisterExtendedBind(keyCode, InputType.DPad, deviceId, direction);
    }

    private void RegisterExtendedBind(string keyCode, InputType type, int id, int direction) {
        if (!_extendedBinds.ContainsKey(keyCode)) {
            _extendedBinds[keyCode] = new List<(InputType, int, int)>();
        }

        _extendedBinds[keyCode].Add((type, id, direction));
    }

    /// <summary>
    /// Handle mouse or controller joystick input event.
    /// </summary>
    public void HandleInputEvent(InputEvent @event) {
        switch (@event) {
            case InputEventMouseMotion mouseMotion:
                _mouseMotionAccumulator -= mouseMotion.Relative;
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown } mouseButton: {
                // Handle mouse wheel input
                var direction = mouseButton.ButtonIndex == MouseButton.WheelUp ? 1f : -1f;
                _mouseWheelAccumulator += direction;
                break;
            }
        }
    }

    /// <summary>
    /// Get the movement vector based on key presses.
    /// </summary>
    public Vector2 GetMoveVector() {
        var temp = Vector2.Zero;
        if (IsKeyPressed(InputKey.MoveForward)) {
            temp.Y -= 1;
        } else if (IsKeyPressed(InputKey.MoveBackward)) {
            temp.Y += 1;
        }

        if (IsKeyPressed(InputKey.MoveLeft)) {
            temp.X -= 1;
        } else if (IsKeyPressed(InputKey.MoveRight)) {
            temp.X += 1;
        }

        // if the controller stick has input, then override the keyboard input
        var stick = GetLeftStickVector();
        if (stick != Vector2.Zero) {
            temp = stick;
        }

        return temp;
    }

    /// <summary>
    /// Get relative mouse or stick motion vector and reset the accumulator.
    /// </summary>
    public Vector2 GetLookVectorAndReset() {
        var temp = _mouseMotionAccumulator;
        _mouseMotionAccumulator = Vector2.Zero;
        // if the controller stick has input, then override the mouse input
        var stick = GetRightStickVector();
        if (stick != Vector2.Zero) {
            temp = stick;
        }

        return temp;
    }

    /// <summary>
    /// Get and reset the mouse wheel accumulator.
    /// </summary>
    public float GetMouseWheelAndReset() {
        var value = _mouseWheelAccumulator;
        _mouseWheelAccumulator = 0f;
        return value;
    }

    /// <summary>
    /// Get the left stick vector from the controller.
    /// </summary>
    private Vector2 GetLeftStickVector() {
        var leftStickX = Input.GetJoyAxis(0, JoyAxis.LeftX);
        var leftStickY = Input.GetJoyAxis(0, JoyAxis.LeftY);

        // Add deadzone to prevent small inputs
        if (Mathf.Abs(leftStickX) < 0.2f) leftStickX = 0;
        if (Mathf.Abs(leftStickY) < 0.2f) leftStickY = 0;

        var vector = new Vector2(leftStickX, leftStickY);
        return vector.Length() > 0.3f ? vector.Normalized() : Vector2.Zero;
    }

    /// <summary>
    /// Get the right stick vector from the controller.
    /// </summary>
    public Vector2 GetRightStickVector() {
        var rightStickX = Input.GetJoyAxis(0, JoyAxis.RightX);
        var rightStickY = Input.GetJoyAxis(0, JoyAxis.RightY);

        // Add deadzone to prevent small inputs
        if (Mathf.Abs(rightStickX) < 0.2f) rightStickX = 0;
        if (Mathf.Abs(rightStickY) < 0.2f) rightStickY = 0;

        var vector = new Vector2(rightStickX, rightStickY);
        return vector.Length() > 0.3f ? vector.Normalized() : Vector2.Zero;
    }

    /// <summary>
    /// Get the D-Pad vector from the controller.
    /// </summary>
    public Vector2 GetDPadVector() {
        var direction = Vector2.Zero;

        // Check D-pad inputs
        if (Input.IsJoyButtonPressed(0, JoyButton.DpadUp)) {
            direction.Y -= 1;
        }

        if (Input.IsJoyButtonPressed(0, JoyButton.DpadDown)) {
            direction.Y += 1;
        }

        if (Input.IsJoyButtonPressed(0, JoyButton.DpadLeft)) {
            direction.X -= 1;
        }

        if (Input.IsJoyButtonPressed(0, JoyButton.DpadRight)) {
            direction.X += 1;
        }

        return direction.Normalized();
    }

    /// <summary>
    /// Check if the key is pressed.
    /// </summary>
    public bool IsKeyPressed(string keyCode) {
        // Check keyboard bindings
        if (_keyBinds.TryGetValue(keyCode, out var keys)) {
            return keys.Any(key => key.Any(Input.IsKeyPressed));
        }

        // Check extended bindings
        if (_extendedBinds.TryGetValue(keyCode, out var binds)) {
            foreach (var (type, id, direction) in binds) {
                switch (type) {
                    // Check joystick button bindings
                    case InputType.JoystickButton when Input.IsJoyButtonPressed(id, (JoyButton)direction):
                        return true;
                    // Check D-pad bindings
                    case InputType.DPad: {
                        var dpadJoyButton = direction switch {
                            (int)DPadDirection.Up => JoyButton.DpadUp,
                            (int)DPadDirection.Right => JoyButton.DpadRight,
                            (int)DPadDirection.Down => JoyButton.DpadDown,
                            (int)DPadDirection.Left => JoyButton.DpadLeft,
                            _ => JoyButton.Invalid
                        };

                        if (Input.IsJoyButtonPressed(id, dpadJoyButton)) {
                            return true;
                        }

                        break;
                    }
                }
                // Mouse wheel is handled through events, not direct polling
            }
        }

        return false;
    }

    public bool IsKeyPressed(string action, InputEvent @event) {
        // Check keyboard bindings
        if (_keyBinds.TryGetValue(action, out var keys)) {
            return keys.Any(key => key.Any(Input.IsKeyPressed)) && @event is InputEventKey keyEvent && keyEvent.Pressed;
        }
        
        // Check extended bindings
        if (_extendedBinds.TryGetValue(action, out var binds)) {
            foreach (var (type, id, direction) in binds) {
                switch (type) {
                    case InputType.JoystickButton when @event is InputEventJoypadButton joypadButton:
                        if (joypadButton.ButtonIndex == (JoyButton)direction && joypadButton.Pressed && joypadButton.Device == id) {
                            return true;
                        }
                        break;
                    case InputType.DPad when @event is InputEventJoypadButton joypadDPad:
                        var dpadJoyButton = direction switch {
                            (int)DPadDirection.Up => JoyButton.DpadUp,
                            (int)DPadDirection.Right => JoyButton.DpadRight,
                            (int)DPadDirection.Down => JoyButton.DpadDown,
                            (int)DPadDirection.Left => JoyButton.DpadLeft,
                            _ => JoyButton.Invalid
                        };
                        if (joypadDPad.ButtonIndex == dpadJoyButton && joypadDPad.Pressed) {
                            return true;
                        }
                        break;
                    case InputType.MouseWheel when @event is InputEventMouseButton mouseWheel:
                        if ((direction > 0 && mouseWheel.ButtonIndex == MouseButton.WheelUp) ||
                            (direction < 0 && mouseWheel.ButtonIndex == MouseButton.WheelDown)) {
                            return mouseWheel.Pressed;
                        }
                        break;
                }
            }
        }

        return false;
    }
}