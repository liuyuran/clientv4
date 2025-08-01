using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using game.scripts.manager.settings;
using game.scripts.manager.settings.configs;
using game.scripts.utils;
using Godot;
using Microsoft.Extensions.Logging;
using ModLoader.setting;

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
        _categoryBox = this.FindNodeByName<VBoxContainer>("CategoryBox");
        _contentBox = this.FindNodeByName<VBoxContainer>("ContentBox");
        _settings = SettingsManager.instance.GetSettings();
        UpdateModSettingsUITranslate();
    }

    private void UpdateModSettingsUITranslate() {
        if (_modPanel == null) return;
        _settings = SettingsManager.instance.GetSettings();
        LoadSettingModules();
    }

    private void LoadSettingModules() {
        var index = 0;
        foreach (var child in _moduleBox.GetChildren()) {
            child.QueueFree();
        }

        foreach (var moduleEntry in _settings) {
            var currentIndex = index++;
            var child = _moduleItemPrototype.Instantiate<Control>();
            _moduleBox.AddChild(child);
            child.FindNodeByName<Button>("ModuleName").Text = SettingsManager.instance.GetModuleName(moduleEntry.Key);
            child.FindNodeByName<Button>("ModuleName").Pressed += () => { LoadSettingCategories(currentIndex); };
        }

        if (_settings.Count == 0) return;
        LoadSettingCategories(0);
    }

    private void InjectExtraButton(Control container, SettingDefine define, Action<string> refreshAction) {
        var config = define.ExtraButtons;
        var extraButtonBox = container.FindNodeByName<HBoxContainer>("ExtraBox");
        foreach (var child in config) {
            var button = new Button();
            extraButtonBox.AddChild(button);
            button.Text = child.Name.Invoke();
            button.Pressed += () => {
                var result = child.OnClick.Invoke();
                if (result != null) {
                    define.OnChange.Invoke(result);
                    refreshAction?.Invoke(result);
                }
            };
        }
    }

    private void LoadSettingCategories(int moduleIndex) {
        var data = _settings;
        var categoryIndex = 0;
        foreach (var moduleEntry in data) {
            if (categoryIndex++ != moduleIndex) continue;
            var module = moduleEntry.Value;
            var children = _categoryBox.GetChildren();
            foreach (var child in children) {
                child.QueueFree();
            }

            children = _contentBox.GetChildren();
            foreach (var child in children) {
                child.QueueFree();
            }

            module.Sort((a, b) => a.Order.CompareTo(b.Order));
            var categoryOffset = new Dictionary<string, int>();
            var categorySet = new List<string>();
            foreach (var config in module) {
                var shouldInsertOffset = false;
                if (!categorySet.Contains(config.Category.Invoke())) {
                    categorySet.Add(config.Category.Invoke());
                    shouldInsertOffset = true;
                }

                var marginBox = new MarginContainer();
                marginBox.AddThemeConstantOverride("margin_bottom", 10);
                switch (config.Config) {
                    case InputSetting inputSetting: {
                        var component = _configInputPrototype.Instantiate<Control>();
                        marginBox.AddChild(component);
                        _contentBox.AddChild(marginBox);
                        component.FindNodeByName<Label>("Key").Text = config.Name.Invoke();
                        var value = component.FindNodeByName<LineEdit>("Value");
                        value.PlaceholderText = inputSetting.Placeholder.Invoke();
                        value.Text = config.Value;
                        if (value.Text.Length == 0) {
                            value.Text = config.DefaultValue.Invoke();
                        }

                        value.TextChanged += text => { config.OnChange.Invoke(text); };
                        InjectExtraButton(component, config, result => {
                            value.Text = result;
                        });

                        if (shouldInsertOffset) {
                            categoryOffset.Add(config.Category.Invoke(), (int)value.GetRect().Position.Y);
                        }

                        break;
                    }
                    case SelectorSetting selectorSetting: {
                        var component = _configSelectorPrototype.Instantiate<Control>();
                        marginBox.AddChild(component);
                        _contentBox.AddChild(marginBox);
                        component.FindNodeByName<Label>("Key").Text = config.Name.Invoke();
                        var value = component.FindNodeByName<OptionButton>("Value");
                        value.AllowReselect = true;
                        var dict = new Dictionary<string, string>();
                        var options = selectorSetting.Options.ToList();
                        foreach (var entry in options) {
                            value.AddItem(entry.Key.Invoke());
                            dict.Add(entry.Key.Invoke(), entry.Value);
                        }

                        value.Selected = options.FindIndex(entry => entry.Value == config.Value);
                        if (value.Selected == -1) {
                            value.Selected = options.FindIndex(entry => entry.Value == config.DefaultValue.Invoke());
                        }

                        value.ItemSelected += item => {
                            var selectedItem = value.GetItemText((int)item);
                            if (selectedItem == config.Value) return;
                            if (!dict.TryGetValue(selectedItem, out var option)) return;
                            config.OnChange.Invoke(option);
                        };
                        InjectExtraButton(component, config, result => {
                            value.Text = result;
                        });

                        if (shouldInsertOffset) {
                            categoryOffset.Add(config.Category.Invoke(), (int)value.GetRect().Position.Y);
                        }

                        break;
                    }
                    case ProgressSetting progressSetting: {
                        var component = _configProgressPrototype.Instantiate<Control>();
                        marginBox.AddChild(component);
                        _contentBox.AddChild(marginBox);
                        component.FindNodeByName<Label>("Key").Text = config.Name.Invoke();
                        var value = component.FindNodeByName<HSlider>("Value");
                        value.MinValue = progressSetting.MinValue;
                        value.MaxValue = progressSetting.MaxValue;
                        try {
                            value.Value = double.Parse(config.Value);
                        } catch (FormatException) {
                            value.Value = double.Parse(config.DefaultValue.Invoke());
                        }

                        value.ValueChanged += newValue => { config.OnChange.Invoke(newValue.ToString(CultureInfo.InvariantCulture)); };
                        InjectExtraButton(component, config, result => {
                            try {
                                value.Value = double.Parse(result);
                            } catch (FormatException) {
                                value.Value = double.Parse(config.DefaultValue.Invoke());
                            }
                        });

                        if (shouldInsertOffset) {
                            categoryOffset.Add(config.Category.Invoke(), (int)value.GetRect().Position.Y);
                        }

                        break;
                    }
                    default:
                        _logger.LogWarning("Unsupported config type: {ConfigType}", config.Config.GetType());
                        break;
                }
            }

            foreach (var category in categorySet) {
                var child = _categoryItemPrototype.Instantiate<Control>();
                _categoryBox.AddChild(child);
                child.FindNodeByName<Button>("CategoryName").Text = category;
                child.FindNodeByName<Button>("CategoryName").Pressed += () => { _contentBox.GetParent<ScrollContainer>().ScrollVertical = categoryOffset[category]; };
            }

            break;
        }
    }
}