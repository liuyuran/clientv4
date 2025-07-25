using System.Collections.Generic;
using game.scripts.manager;
using game.scripts.utils;
using Godot;
using ModLoader.setting;

namespace game.scripts.start;

public partial class Menu {
    [Export] private PackedScene _settingPanelScene;
    [Export] private PackedScene _moduleItemPrototype;
    [Export] private PackedScene _categoryItemPrototype;
    [Export] private PackedScene _configInputPrototype;
    [Export] private PackedScene _configSelectorPrototype;
    [Export] private PackedScene _configRadioPrototype;
    private Control _settingPanel;
    private Control _moduleBox;
    private Control _categoryBox;
    private Control _contentBox;
    private Dictionary<string, List<SettingDefine>> _settings;

    private void CloseSettingPanel() {
        if (_settingPanel == null) return;
        _settingPanel.QueueFree();
        _settingPanel = null;
        _modalPanel.Visible = false;
    }

    private void OpenSettingPanel() {
        _settingPanel = _settingPanelScene.Instantiate<Control>();
        _modalPanel.AddChild(_settingPanel);
        _moduleBox = this.FindNodeByName<HBoxContainer>("ModuleBox");
        _categoryBox = this.FindNodeByName<HBoxContainer>("CategoryBox");
        _contentBox = this.FindNodeByName<VBoxContainer>("ContentBox");
        _settings = SettingsManager.instance.GetCoreSettings();
        LoadSettingModules();
    }
}