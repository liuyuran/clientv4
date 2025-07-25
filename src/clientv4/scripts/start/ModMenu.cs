using game.scripts.manager;
using game.scripts.utils;
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
        _moduleBox = this.FindNodeByName<HBoxContainer>("ModuleBox");
        _categoryBox = this.FindNodeByName<HBoxContainer>("CategoryBox");
        _contentBox = this.FindNodeByName<VBoxContainer>("ContentBox");
        _settings = SettingsManager.instance.GetSettings();
        LoadSettingModules();
    }

    private void LoadSettingModules() {
        var data = SettingsManager.instance.GetSettings();
        LoadSettingCategories(0);
    }
    
    private void LoadSettingCategories(int index) {}
}