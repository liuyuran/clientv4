using Friflo.Engine.ECS;
using game.scripts.config;
using game.scripts.server.ECSBridge.input;
using Godot;

namespace game.scripts;

public partial class PlayerControl(Entity inputHandler) : CharacterBody3D {
	private CInputEvent _lastEvents;
	private CInputEvent _events;

	public override void _Ready() {
		Input.MouseMode = Input.MouseModeEnum.Captured;
		ProcessMode = ProcessModeEnum.Always;
	}

	public override void _Input(InputEvent @event) {
		InputManager.instance.HandleInputEvent(@event);
	}

	public override void _Process(double delta) {
		if (GetTree().Paused) {
			Rpc(MethodName.UpdateInputEvent,
				Vector3.Zero,
				Vector3.Zero, 
				false,
				false,
				Vector2.Zero
			);
			ProcessMode = ProcessModeEnum.Pausable;
			return;
		}
		ProcessMode = ProcessModeEnum.Always;
		var currentFrameEvents = new CInputEvent {
			ForwardVector = InputManager.instance.GetLookVectorAndReset()
		};

		var moveVector = InputManager.instance.GetMoveVector();
		currentFrameEvents.MoveForward = moveVector.Y < 0;
		currentFrameEvents.MoveBackward = moveVector.Y > 0;
		currentFrameEvents.MoveLeft = moveVector.X < 0;
		currentFrameEvents.MoveRight = moveVector.X > 0;
		currentFrameEvents.Jump = InputManager.instance.IsKeyPressed(InputKey.Jump);
		currentFrameEvents.Crouch = InputManager.instance.IsKeyPressed(InputKey.Crouch);
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
				moveVector.X,
				_events.Jump ? 1 : _events.Crouch ? -1 : 0,
				moveVector.Y
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
