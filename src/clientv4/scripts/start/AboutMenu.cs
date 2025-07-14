using Godot;

namespace game.scripts.start;

public partial class Menu {
    [Export] private PackedScene _aboutPanelScene;
    private Control _aboutPanel;
    private RichTextLabel _aboutLabel;
    
    private void CloseAboutPanel() {
        if (_aboutPanel == null) return;
        _aboutPanel.QueueFree();
        _aboutPanel = null;
    }

    private void OpenAboutPanel() {
        _aboutPanel = _aboutPanelScene.Instantiate<Control>();
        _modalPanel.AddChild(_aboutPanel);
        _aboutLabel = _aboutPanel.GetNode<RichTextLabel>("Back/Text");
        _aboutLabel.Text = GetAboutText() + "\n\n";
    }

    private string GetAboutText() {
        return "This is a demo game for Godot 4.2.";
    }
}