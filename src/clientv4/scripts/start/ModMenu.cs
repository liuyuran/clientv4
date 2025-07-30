using System;
using System.Collections.Generic;
using System.Globalization;
using game.scripts.manager.settings;
using game.scripts.manager.settings.configs;
using game.scripts.utils;
using Godot;
using Microsoft.Extensions.Logging;

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
        LoadSettingModules();
    }

    private void LoadSettingModules() {
        var index = 0;
        foreach (var moduleEntry in _settings) {
            var currentIndex = index++;
            var child = _moduleItemPrototype.Instantiate<Control>();
            _moduleBox.AddChild(child);
            child.FindNodeByName<Button>("ModuleName").Text = moduleEntry.Key;
            child.FindNodeByName<Button>("ModuleName").Pressed += () => {
                LoadSettingCategories(currentIndex);
            };
        }
        if (_settings.Count == 0) return;
        LoadSettingCategories(0);
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

            module.Sort((a, b) => {
                var result = string.Compare(a.Category, b.Category, StringComparison.Ordinal);
                if (result != 0) return result;
                return a.Order.CompareTo(b.Order);
            });
            var categoryOffset = new Dictionary<string, int>();
            var categorySet = new List<string>();
            foreach (var config in module) {
                var shouldInsertOffset = false;
                if (!categorySet.Contains(config.Category)) {
                    categorySet.Add(config.Category);
                    shouldInsertOffset = true;
                }
                switch (config.Config) {
                    case InputSetting inputSetting: {
                        var component = _configInputPrototype.Instantiate<Control>();
                        _contentBox.AddChild(component);
                        component.FindNodeByName<Label>("Key").Text = config.Name;
                        var value = component.FindNodeByName<LineEdit>("Value");
                        value.PlaceholderText = inputSetting.Placeholder;
                        value.Text = config.Value;
                        if (value.Text.Length == 0) {
                            value.Text = config.DefaultValue;
                        }

                        value.TextChanged += text => {
                            config.OnChange.Invoke(text);
                        };
                        if (shouldInsertOffset) {
                            categoryOffset.Add(config.Category, (int)value.GetRect().Position.Y);
                        }
                        break;
                    }
                    case SelectorSetting selectorSetting: {
                        var component = _configSelectorPrototype.Instantiate<Control>();
                        _contentBox.AddChild(component);
                        component.FindNodeByName<Label>("Key").Text = config.Name;
                        var value = component.FindNodeByName<OptionButton>("Value");
                        value.AllowReselect = true;
                        foreach (var entry in selectorSetting.Options) {
                            value.AddItem(entry.Key);
                        }

                        value.ItemSelected += item => {
                            var selectedItem = value.GetItemText((int)item);
                            if (selectedItem == config.Value) return;
                            if (!selectorSetting.Options.TryGetValue(selectedItem, out var option)) return;
                            config.OnChange.Invoke(option);
                        };
                        
                        if (shouldInsertOffset) {
                            categoryOffset.Add(config.Category, (int)value.GetRect().Position.Y);
                        }
                        break;
                    }
                    case ProgressSetting progressSetting: {
                        var component = _configProgressPrototype.Instantiate<Control>();
                        _contentBox.AddChild(component);
                        component.FindNodeByName<Label>("Key").Text = config.Name;
                        var value = component.FindNodeByName<HSlider>("Value");
                        value.MinValue = progressSetting.MinValue;
                        value.MaxValue = progressSetting.MaxValue;
                        try {
                            value.Value = double.Parse(config.Value);
                        } catch (FormatException) {
                            value.Value = double.Parse(config.DefaultValue);
                        }

                        value.ValueChanged += newValue => {
                            config.OnChange.Invoke(newValue.ToString(CultureInfo.InvariantCulture));
                        };
                        
                        if (shouldInsertOffset) {
                            categoryOffset.Add(config.Category, (int)value.GetRect().Position.Y);
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
                child.FindNodeByName<Button>("CategoryName").Pressed += () => {
                    _contentBox.GetParent<ScrollContainer>().ScrollVertical = categoryOffset[category];
                };
            }
            break;
        }
    }
}