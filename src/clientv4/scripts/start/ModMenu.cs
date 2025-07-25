using Godot;

namespace game.scripts.start;

public partial class Menu {
    [Export] private PackedScene _modPanelScene;
    private Control _modPanel;
    
    private void CloseModPanel() {
        if (_modPanel == null) return;
        _modPanel.QueueFree();
        _modPanel = null;
        _modalPanel.Visible = false;
    }
    
    private void OpenModPanel() {
        _modPanel = _modPanelScene.Instantiate<Control>();
        _modalPanel.AddChild(_modPanel);
        LoadModSettingModules();
    }

    private void LoadModSettingModules() {
        LoadModSettingCategories(0);
    }
    
    private void LoadModSettingCategories(int index) {}
}