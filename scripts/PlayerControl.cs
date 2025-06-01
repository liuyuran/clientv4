using Friflo.Engine.ECS;
using game.scripts.server.ECSBridge.input;
using Godot;

namespace game.scripts;

public partial class PlayerControl(Entity inputHandler) : CharacterBody3D {
    private const float MouseSpeed = 0.1f;
    private CInputEvent _lastEvents;
    private CInputEvent _events;
    private readonly CInputEvent _emptyEvents = new();
    private Mutex _mutex = new();
    private ulong _eventUpdateTime;

    public override void _Ready() {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Process(double delta) {
        // Update the input handler with the latest input events
        _mutex.Lock();
        if (_eventUpdateTime + 100 < Time.GetTicksMsec()) {
            _events = _emptyEvents;
        }

        if (_events == _lastEvents) {
            _mutex.Unlock();
            return;
        }
        Rpc(MethodName.UpdateInputEvent,
            new Vector3(
                _events.MoveLeft ? -1 : _events.MoveRight ? 1 : 0,
                _events.Jump ? 1 : _events.Crouch ? -1 : 0,
                _events.MoveForward ? -1 : _events.MoveBackward ? 1 : 0
            ),
            _events.ForwardVector,
            _events.Digging,
            _events.Placing,
            _events.MouseClickPosition
        );
        _lastEvents = _events;
        _mutex.Unlock();
    }

    public override void _Input(InputEvent @event) {
        var events = new CInputEvent();
        switch (@event) {
            case InputEventKey eventKey when eventKey.IsPressed(): {
                GD.Print("Key pressed: ", eventKey.Keycode);
                if (eventKey.Keycode == Key.Escape) {
                    Input.MouseMode = Input.MouseModeEnum.Visible;
                }

                switch (eventKey.Keycode) {
                    case Key.W:
                        events.MoveForward = true;
                        break;
                    case Key.S:
                        events.MoveBackward = true;
                        break;
                    case Key.A:
                        events.MoveLeft = true;
                        break;
                    case Key.D:
                        events.MoveRight = true;
                        break;
                    case Key.Space:
                        events.Jump = true;
                        break;
                    case Key.Ctrl:
                        events.Crouch = true;
                        break;
                }

                break;
            }
            case InputEventMouseButton eventMouseButton when eventMouseButton.IsPressed():
                events.MouseClickPosition = eventMouseButton.Position;
                switch (eventMouseButton.ButtonIndex) {
                    case MouseButton.Left: {
                        events.Digging = true;
                        break;
                    }
                    case MouseButton.Right: {
                        events.Placing = true;
                        break;
                    }
                }

                break;
        }

        if (@event is InputEventMouseMotion eventMouseMotion) {
            var relative = eventMouseMotion.Relative;
            events.ForwardVector = new Vector2(
                -relative.X * MouseSpeed,
                -relative.Y * MouseSpeed
            );
        } else {
            events.ForwardVector = Vector2.Zero;
        }

        _mutex.Lock();
        _eventUpdateTime = Time.GetTicksMsec();
        _events = events;
        _mutex.Unlock();
    }

    [Rpc(CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void UpdateInputEvent(Vector3 forward, Vector2 mouseRelative, bool digging, bool placing, Vector2 mouseClickPosition) {
        inputHandler.GetComponent<CInputEvent>().MoveLeft = forward.X < 0;
        inputHandler.GetComponent<CInputEvent>().MoveRight = forward.X > 0;
        inputHandler.GetComponent<CInputEvent>().MoveForward = forward.Z < 0;
        inputHandler.GetComponent<CInputEvent>().MoveBackward = forward.Z > 0;
        inputHandler.GetComponent<CInputEvent>().Jump = forward.Y > 0;
        inputHandler.GetComponent<CInputEvent>().Crouch = forward.Y < 0;
        inputHandler.GetComponent<CInputEvent>().ForwardVector = new Vector2(
            Mathf.DegToRad(mouseRelative.X),
            Mathf.DegToRad(mouseRelative.Y)
        );
        inputHandler.GetComponent<CInputEvent>().Digging = digging;
        inputHandler.GetComponent<CInputEvent>().Placing = placing;
        inputHandler.GetComponent<CInputEvent>().MouseClickPosition = mouseClickPosition;
    }
}