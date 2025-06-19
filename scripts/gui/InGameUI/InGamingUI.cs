using Godot;

namespace game.scripts.gui.InGameUI;

public partial class InGamingUI: CanvasLayer {
	[Export] public PackedScene PlayingUI;
	[Export] public PackedScene PauseUI;
	[Export] public PackedScene MenusUI;
	private InGamingUIConfig _config;
		
	public override void _Ready() {
		OpenPlayingUI();
	}

	public override void _Process(double delta) {
		UpdatePlayingUI(delta);
	}

	public override void _Input(InputEvent @event) {
		HandleInputOnPlayerUI(@event);
	}
}
