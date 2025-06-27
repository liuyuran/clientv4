using System;
using Godot;

namespace game.scripts.gui.InGameUI;

public partial class InGamingUI: CanvasLayer {
	[Export] public PackedScene PlayingUI;
	[Export] public PackedScene PauseUI;
	[Export] public PackedScene MenusUI;
	private InGamingUIStatus _status;
		
	public override void _Ready() {
		ProcessMode = ProcessModeEnum.Always;
		_status.Focus = InGameUIFocus.Game;
		OpenPlayingUI();
	}

	public override void _Process(double delta) {
		switch (_status.Focus) {
			case InGameUIFocus.Game:
				UpdatePlayingUI(delta);
				TryOpenPauseUI();
				break;
			case InGameUIFocus.Pause:
				TryClosePauseUI();
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	public override void _Input(InputEvent @event) {
		switch (_status.Focus) {
			case InGameUIFocus.Game:
				HandleInputOnPlayerUI(@event);
				break;
			case InGameUIFocus.Pause:
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}
}
