using Godot;

namespace game.scripts.start;

public partial class Menu {
    [Export] private PackedScene _aboutPanelScene;
    private Panel _aboutPanel;
    
    private void CloseAboutPanel() {
        if (_aboutPanel != null) {
            _aboutPanel.QueueFree();
            _aboutPanel = null;
        }
    }

    private void OpenAboutPanel() {
        _aboutPanel = _aboutPanelScene.Instantiate<Panel>();
        AddChild(_aboutPanel);
    }
}