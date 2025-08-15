using game.scripts.utils;
using Godot;

namespace game.scripts.renderer;

public partial class Loading : CanvasLayer {
	private ColorRect _background;
	private Control _content;
	private RichTextLabel _text;

	public override void _Ready() {
		FollowViewportEnabled = true;
		_background = GetNode<ColorRect>("Background");
		_content = GetNode<Control>("Content");
		_text = GetNode<RichTextLabel>("Content/Text");
		GetTree().Root.SizeChanged += OnRootSizeChanged;
		CallDeferred(MethodName.OnRootSizeChanged);
	}

	private void OnRootSizeChanged() {
		_content.Size = GetTree().Root.Size;
		_text.Size = new Vector2(_content.Size.X, 30);
		_background.Size = GetTree().Root.Size;
		_content.Position = Vector2.Zero;
		_background.Position = Vector2.Zero;
		_text.Position = new Vector2(0, _content.Size.Y / 2 - _text.Size.Y / 2);
	}

	public override void _Process(double delta) {
		switch (GameStatus.currentStatus) {
			case GameStatus.Status.Loading:
				Visible = true;
				_text.Text = "[center]Loading...[/center]";
				break;
			case GameStatus.Status.Playing:
			case GameStatus.Status.StartMenu:
			default:
				Visible = false;
				break;
		}
	}
}
