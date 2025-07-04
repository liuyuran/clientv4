using Godot;

namespace game.scripts.start;

public partial class Menu {
    [Export] private PackedScene _settingPanelScene;
    private Panel _settingPanel;

    private void CloseSettingPanel() {
        if (_settingPanel != null) {
            _settingPanel.QueueFree();
            _settingPanel = null;
        }
    }

    private void OpenSettingPanel() {
        _settingPanel = _settingPanelScene.Instantiate<Panel>();
        AddChild(_settingPanel);
    }
}