using System;
using game.scripts.config;
using game.scripts.manager;
using Godot;

namespace game.scripts.gui.InGameUI;

public partial class InGamingUI: CanvasLayer {
	[Export] public PackedScene PlayingUI;
	[Export] public PackedScene PauseUI;
	[Export] public PackedScene MenusUI;
	private InGamingUIStatus _status;
		
	public override void _Ready() {
		_status.Focus = InGameUIFocus.Game;
		OpenPlayingUI();
		MenuManager.instance.AddMenuGroup("player", 1);
		MenuManager.instance.AddMenuGroup("inventory", 1);
		MenuManager.instance.AddMenuItem("player", "player", "哈哈哈", 1, "", () => {
			GD.Print("testA");
		});
		MenuManager.instance.AddMenuItem("inventory", "inventory", "你大爷的", 1, "", () => {
			GD.Print("testB");
		});
	}

	public override void _Process(double delta) {
		switch (_status.Focus) {
			case InGameUIFocus.Game:
				UpdatePlayingUI(delta);
				break;
			case InGameUIFocus.Pause:
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	public override void _Input(InputEvent @event) {
		switch (_status.Focus) {
			case InGameUIFocus.Game:
				HandleInputOnPlayerUI(@event);
				TryOpenPauseUI(@event);
				break;
			case InGameUIFocus.Pause:
				TryClosePauseUI(@event);
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}
	
	private void TryOpenPauseUI(InputEvent @event) {
		if (_status.Focus == InGameUIFocus.Pause) {
			return;
		}
		if (InputManager.instance.IsKeyPressed(@event, InputKey.SwitchPause)) {
			Input.MouseMode = Input.MouseModeEnum.Visible;
			AddChild(PauseUI.Instantiate<Control>());
		}
	}
	
	private void TryClosePauseUI(InputEvent @event) {
		if (_status.Focus != InGameUIFocus.Pause) {
			return;
		}
		if (InputManager.instance.IsKeyPressed(@event, InputKey.SwitchPause)) {
			Input.MouseMode = Input.MouseModeEnum.Captured;
			GetTree().Root.RemoveChild(this);
			_status.Focus = InGameUIFocus.Game;
		}
	}
}
