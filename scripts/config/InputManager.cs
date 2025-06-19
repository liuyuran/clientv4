using System.Collections.Generic;
using Godot;

namespace game.scripts.config;

/// <summary>
/// Check input mapping and filter input events.
/// </summary>
public class InputManager {
    public static InputManager instance { get; private set; } = new();
    private readonly Dictionary<string, List<Key[]>> _keyBinds = new();
    private Vector2 _mouseMotionAccumulator = Vector2.Zero;

    private InputManager() {
        RegistryKeyBind(InputKey.MoveForward, Key.W);
        RegistryKeyBind(InputKey.MoveBackward, Key.S);
        RegistryKeyBind(InputKey.MoveLeft, Key.A);
        RegistryKeyBind(InputKey.MoveRight, Key.D);
        RegistryKeyBind(InputKey.Jump, Key.Space);
        RegistryKeyBind(InputKey.Crouch, Key.Ctrl);
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
    /// Handle mouse or controller joystick input event.
    /// </summary>
    public void HandleInputEvent(InputEvent @event) {
        if (@event is InputEventMouseMotion mouseMotion) {
            _mouseMotionAccumulator -= mouseMotion.Relative;
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
    /// Get the left stick vector from the controller.
    /// </summary>
    public Vector2 GetLeftStickVector() {
        var leftStickX = Input.GetJoyAxis(0, JoyAxis.LeftX);
        var leftStickY = Input.GetJoyAxis(0, JoyAxis.LeftY);
        return new Vector2(leftStickX, leftStickY).Normalized();
    }
    
    /// <summary>
    /// Get the right stick vector from the controller.
    /// </summary>
    public Vector2 GetRightStickVector() {
        var rightStickX = Input.GetJoyAxis(0, JoyAxis.RightX);
        var rightStickY = Input.GetJoyAxis(0, JoyAxis.RightY);
        return new Vector2(rightStickX, rightStickY).Normalized();
    }

    /// <summary>
    /// Check if the key is pressed.
    /// </summary>
    public bool IsKeyPressed(InputEvent @event, string keyCode) {
        if (@event is not InputEventKey eventKey) return false;
        if (!_keyBinds.TryGetValue(keyCode, out var keys)) {
            return false;
        }
        foreach (var key in keys) {
            if (eventKey.IsPressed() && eventKey.Keycode == key[0]) {
                if (key.Length > 1) {
                    for (var i = 1; i < key.Length; i++) {
                        if (!Input.IsKeyPressed(key[i])) {
                            return false;
                        }
                    }
                }
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Check if the key is pressed.
    /// </summary>
    public bool IsKeyPressed(string keyCode) {
        if (!_keyBinds.TryGetValue(keyCode, out var keys)) {
            return false;
        }
        foreach (var key in keys) {
            if (Input.IsKeyPressed(key[0])) {
                if (key.Length > 1) {
                    for (var i = 1; i < key.Length; i++) {
                        if (!Input.IsKeyPressed(key[i])) {
                            return false;
                        }
                    }
                }
                return true;
            }
        }
        return false;
    }
}