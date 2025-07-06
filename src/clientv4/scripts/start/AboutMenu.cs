using Godot;

namespace game.scripts.start;

public partial class Menu {
    [Export] private PackedScene _aboutPanelScene;
    private Control _aboutPanel;
    
    private void CloseAboutPanel() {
        if (_aboutPanel == null) return;
        _aboutPanel.QueueFree();
        _aboutPanel = null;
    }

    private void OpenAboutPanel() {
        _aboutPanel = _aboutPanelScene.Instantiate<Control>();
        _modalPanel.AddChild(_aboutPanel);
    }
}