using System;
using game.scripts.manager;
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
		MenuManager.instance.AddMenuGroup("player", 1);
		MenuManager.instance.AddMenuGroup("inventory", 2);
		MenuManager.instance.AddMenuGroup("test", 3);
		MenuManager.instance.AddMenuItem("player", "player", "哈哈哈", 1, "", () => {
			GD.Print("testA");
		});
		MenuManager.instance.AddMenuItem("inventory", "inventory", "你YY的", 1, "", () => {
			GD.Print("testB");
		});
		MenuManager.instance.AddMenuItem("inventory", "inventory2", "你XX的", 2, "", () => {
			GD.Print("testB");
		});
		MenuManager.instance.AddMenuItem("test", "test", "测试", 1, "", () => {
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
}
