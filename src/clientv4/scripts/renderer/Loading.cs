using Godot;

namespace game.scripts.renderer;

public partial class Loading : CanvasLayer {
	private ColorRect _background;
	private Control _content;
	private RichTextLabel _text;

	public override void _Ready() {
		_background = GetNode<ColorRect>("Background");
		_content = GetNode<Control>("Content");
		_text = GetNode<RichTextLabel>("Content/Text");
		// Set the background color to black
		_background.Color = new Color(0, 0, 0, 0.5f);
		// Center the content
		_content.Position = GetTree().Root.Size;
		// Set the text to loading
		_text.Text = "Loading...Loading...Loading...Loading...";
		_text.Position = (GetTree().Root.Size - _text.Size) / 2;
	}
}
