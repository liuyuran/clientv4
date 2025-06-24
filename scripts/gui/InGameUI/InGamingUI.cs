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
	private ulong _lastPauseTime;
	private Control _pauseUI;
		
	public override void _Ready() {
		ProcessMode = ProcessModeEnum.Always;
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
	
	private void TryOpenPauseUI() {
		if (_status.Focus == InGameUIFocus.Pause) {
			return;
		}
		if (InputManager.instance.IsKeyPressed(InputKey.SwitchPause) && _pauseUI == null && Time.GetTicksMsec() - _lastPauseTime > 500) {
			Input.MouseMode = Input.MouseModeEnum.Visible;
			_pauseUI = PauseUI.Instantiate<Control>();
			AddChild(_pauseUI);
			_status.Focus = InGameUIFocus.Pause;
			_lastPauseTime = Time.GetTicksMsec();
		}
	}
	
	private void TryClosePauseUI() {
		if (_status.Focus != InGameUIFocus.Pause) {
			return;
		}
		if (InputManager.instance.IsKeyPressed(InputKey.SwitchPause) && _pauseUI != null && Time.GetTicksMsec() - _lastPauseTime > 500) {
			Input.MouseMode = Input.MouseModeEnum.Captured;
			RemoveChild(_pauseUI);
			_pauseUI = null;
			_status.Focus = InGameUIFocus.Game;
			_lastPauseTime = Time.GetTicksMsec();
		}
	}
}
