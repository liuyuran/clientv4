using Friflo.Engine.ECS;
using game.scripts.server.ECSBridge.input;
using Godot;

namespace game.scripts;

public partial class PlayerControl(Entity inputHandler) : CharacterBody3D {
    private CInputEvent _lastEvents;
    private CInputEvent _events;
    private Vector2 _mouseMotionAccumulator = Vector2.Zero;

    public override void _Ready() {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent @event) {
        if (@event is InputEventMouseMotion mouseMotion) {
            _mouseMotionAccumulator += mouseMotion.Relative;
        }
    }

    public override void _Process(double delta) {
        var currentFrameEvents = new CInputEvent {
            ForwardVector = -_mouseMotionAccumulator
        };

        _mouseMotionAccumulator = Vector2.Zero; // Reset for the next frame

        // Keyboard input
        if (Input.IsActionPressed("ui_cancel")) { // Assuming Escape is mapped to ui_cancel
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }

        currentFrameEvents.MoveForward = Input.IsKeyPressed(Key.W);
        currentFrameEvents.MoveBackward = Input.IsKeyPressed(Key.S);
        currentFrameEvents.MoveLeft = Input.IsKeyPressed(Key.A);
        currentFrameEvents.MoveRight = Input.IsKeyPressed(Key.D);
        currentFrameEvents.Jump = Input.IsKeyPressed(Key.Space);
        currentFrameEvents.Crouch = Input.IsKeyPressed(Key.Ctrl);
        currentFrameEvents.Digging = Input.IsMouseButtonPressed(MouseButton.Left);
        currentFrameEvents.Placing = Input.IsMouseButtonPressed(MouseButton.Right);

        if (currentFrameEvents.Digging || currentFrameEvents.Placing) {
            currentFrameEvents.MouseClickPosition = GetViewport().GetMousePosition();
        }

        _events = currentFrameEvents;

        if (_events == _lastEvents) {
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
