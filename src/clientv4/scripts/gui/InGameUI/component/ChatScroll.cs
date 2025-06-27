using game.scripts.manager;
using game.scripts.manager.chat;
using Godot;

namespace game.scripts.gui.InGameUI.component;

public partial class ChatScroll: Panel {
	public InGamingUI InGamingUIInstance;
	private ScrollContainer _chatHistory;
	private VBoxContainer _chatBox;
	private Tween _tween;
	private const int MaxLine = 300;
	
	public override void _Ready() {
		_chatHistory = GetNode<ScrollContainer>("chatHistory");
		_chatBox = _chatHistory.GetNode<VBoxContainer>("chatBox");
		AddMessage(new ChatManager.MessageInfo {
			Message = "[color=yellow]Welcome to Friflo![/color]"
		});
		ScrollToBottom();
		ChatManager.instance.OnMessageAdded += AddMessage;
	}

	public override void _ExitTree() {
		ChatManager.instance.OnMessageAdded -= AddMessage;
		_tween?.Stop();
		base._ExitTree();
	}

	private void AddMessage(ChatManager.MessageInfo message) {
		var label = new RichTextLabel();
		label.Text = message.Message;
		label.AutowrapMode = TextServer.AutowrapMode.Word;
		label.BbcodeEnabled = true;
		label.FitContent = true;
		label.ScrollActive = false;
		label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		label.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		_chatBox.AddChild(label);
		if (_chatBox.GetChildCount() > MaxLine) {
			_chatBox.RemoveChild(_chatBox.GetChild(0));
		}
		ScrollToBottomSmoothly();
	}

	private void SendMessage() {
		InGamingUIInstance.BroadcastChatMessage("");
	}

	private void ScrollToBottom() {
		var currentScroll = _chatHistory.GetVScrollBar().Value;
		var targetScroll = _chatBox.GetRect().Size.Y - GetRect().Size.Y;
		if (currentScroll < targetScroll) {
			_chatHistory.GetVScrollBar().Value = targetScroll;
		}
	}
	
	private void ScrollToBottomSmoothly() {
		if (_tween != null && _tween.IsRunning()) {
			_tween.Stop();
		} else {
			_tween = GetTree().CreateTween();
		}
		var targetScroll = _chatBox.GetRect().Size.Y - GetRect().Size.Y;
		_tween.TweenProperty(_chatHistory.GetVScrollBar(), "value", targetScroll, 0.5f)
			.SetTrans(Tween.TransitionType.Back);
		_tween.Play();
	}
}
