using Godot;

namespace game.scripts.start;

public partial class Menu {
    [Export] private PackedScene _settingPanelScene;
    private Control _settingPanel;

    private void CloseSettingPanel() {
        if (_settingPanel == null) return;
        _settingPanel.QueueFree();
        _settingPanel = null;
    }

    private void OpenSettingPanel() {
        _settingPanel = _settingPanelScene.Instantiate<Control>();
        _modalPanel.AddChild(_settingPanel);
    }
}