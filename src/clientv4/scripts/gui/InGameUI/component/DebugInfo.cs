using Godot;

namespace game.scripts.gui.InGameUI.component;

public partial class DebugInfo: Panel {
    private RichTextLabel _fps;

    public override void _Ready() {
        _fps = GetNode<RichTextLabel>("FPS");
        _fps.Text = "FPS: 0";
    }
    
    public override void _Process(double delta) {
        _fps.Text = $"FPS: {Engine.GetFramesPerSecond()}";
    }
}