using System;
using game.scripts.config;
using game.scripts.utils;
using Godot;

namespace game.scripts.gui.InGameUI;

public partial class InGamingUI: CanvasLayer {
	[Export] public PackedScene PlayingUI;
	[Export] public PackedScene PauseUI;
	[Export] public PackedScene MenusUI;
	private InGamingUIStatus _status;
	private LineEdit _msgInput;
		
	public override void _Ready() {
		GameNodeReference.UI = this;
		ProcessMode = ProcessModeEnum.Always;
		_status.Focus = InGameUIFocus.Game;
		OpenPlayingUI();
		_msgInput = this.FindNodeByName<LineEdit>("MsgInput");
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
		if (_status.Focus != InGameUIFocus.Game && 
		    GameStatus.currentStatus != GameStatus.Status.Playing &&
		    GameStatus.currentStatus != GameStatus.Status.Typing) return;
		if (!InputManager.instance.IsKeyPressed(InputKey.UIConfirm, @event)) return;
		if (_msgInput.HasFocus()) {
			_msgInput.ReleaseFocus();
			GameStatus.SetStatus(GameStatus.Status.Playing);
		} else {
			_msgInput.GrabFocus();
			GameStatus.SetStatus(GameStatus.Status.Typing);
		}
	}
}
