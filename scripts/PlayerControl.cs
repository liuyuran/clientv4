using System;
using Godot;

namespace game.scripts;

public partial class PlayerControl : CharacterBody3D {
	private const float MouseSpeed = 0.1f;
	private Node3D _head;
	
	public override void _Ready() {
		_head = GetNode<Node3D>("head");
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Process(double delta) {
		//
	}

	public override void _Input(InputEvent @event) {
		if (@event is InputEventKey eventKey && eventKey.IsPressed()) {
			GD.Print("Key pressed: ", eventKey.Keycode);
			if (eventKey.Keycode == Key.Escape) {
				Input.MouseMode = Input.MouseModeEnum.Visible;
			}
			Velocity = Vector3.Zero;
			switch (eventKey.Keycode) {
				case Key.W:
					Velocity += -Basis.Z * 10;
					break;
				case Key.S:
					Velocity += Basis.Z * 10;
					break;
				case Key.A:
					Velocity += -Basis.X * 10;
					break;
				case Key.D:
					Velocity += Basis.X * 10;
					break;
				case Key.Space:
					Velocity += Basis.Y * 10;
					break;
				case Key.Ctrl:
					Velocity += -Basis.Y * 10;
					break;
			}

			MoveAndSlide();
		}
		if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.IsPressed()) {
			GD.Print("Mouse button pressed: ", eventMouseButton.ButtonIndex);
		}

		if (@event is not InputEventMouseMotion eventMouseMotion) return;
		var relative = eventMouseMotion.Relative;
		RotateY(Mathf.DegToRad(-relative.X * MouseSpeed));
		_head.RotateX(Mathf.DegToRad(-relative.Y * MouseSpeed));
	}
}
