using Godot;

namespace game.scripts.start;

public partial class Menu {
    [Export] private PackedScene _modPanelScene;
    private Panel _modPanel;
    
    private void CloseModPanel() {
        if (_modPanel != null) {
            _modPanel.QueueFree();
            _modPanel = null;
        }
    }
    
    private void OpenModPanel() {
        _modPanel = _modPanelScene.Instantiate<Panel>();
        AddChild(_modPanel);
    }
}