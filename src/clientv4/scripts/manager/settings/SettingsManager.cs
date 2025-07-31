using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using game.scripts.manager.archive;
using game.scripts.manager.reset;
using game.scripts.manager.settings.configs;
using Godot;
using Microsoft.Extensions.Logging;
using ModLoader;
using ModLoader.archive;
using ModLoader.handler;
using ModLoader.logger;
using ModLoader.setting;
using FileAccess = Godot.FileAccess;

namespace game.scripts.manager.settings;

public class SettingsManager : ISettingsManager, IDisposable, IReset, IArchive {
    private readonly ILogger _logger = LogManager.GetLogger<SettingsManager>();
    public static SettingsManager instance { get; private set; } = new();
    private readonly Dictionary<string, List<SettingDefine>> _settings = new();
    private readonly Dictionary<string, GetString> _moduleNameLinks = new();
    private const string SettingsFile = "settings.json";
    private const string CoreSetting = "core";

    private SettingsManager() {
        AddModuleNameLink(CoreSetting, () => I18N.Tr("core.gui", "settings.module.core"));
        AddCoreSetting(new SettingDefine {
            Key = "language",
            Category = () => I18N.Tr("core.gui", "settings.category.language"),
            Config = new SelectorSetting {
                Options = new Dictionary<GetString, string> {
                    {() => "中文", "zh_CN"}
                }
            },
            Value = "zh_CN",
            DefaultValue = () => "zh_CN",
            Name = () => I18N.Tr("core.gui", "settings.language"),
            Description = () => I18N.Tr("core.gui", "settings.language.description"),
            OnChange = value => {
                LanguageManager.instance.SetLanguage(value);
                SaveCoreSettings();
            },
            Order = 0
        });
        AddCoreSetting(new SettingDefine {
            Key = "game.nickname",
            Category = () => I18N.Tr("core.gui", "settings.category.game"),
            Config = new InputSetting {
                Placeholder = () => I18N.Tr("core.gui", "settings.game.nickname.placeholder"),
            },
            Value = "",
            DefaultValue = () => "",
            Name = () => I18N.Tr("core.gui", "settings.game.nickname"),
            Description = () => I18N.Tr("core.gui", "settings.game.nickname.description"),
            OnChange = value => {
                LanguageManager.instance.ReloadLanguageFiles();
                TranslationServer.SetLocale(value);
                SaveCoreSettings();
            },
            Order = 0
        });
    }
    
    public void AddCoreSetting(SettingDefine setting) {
        AddSetting(CoreSetting, setting);
    }
    
    public void AddSetting(string module, SettingDefine setting) {
        if (!_settings.ContainsKey(module)) {
            _settings[module] = [];
        }
        _settings[module].Add(setting);
    }

    public string GetModuleName(string module) {
        if (_moduleNameLinks.TryGetValue(module, out var link)) {
            return link.Invoke();
        }
        return module;
    }

    public void AddModuleNameLink(string module, GetString link) {
        _moduleNameLinks.TryAdd(module, link);
    }
    
    public Dictionary<string, List<SettingDefine>> GetCoreSettings() {
        var settingsCopy = new Dictionary<string, List<SettingDefine>>();
        foreach (var kvp in _settings) {
            if (kvp.Key != CoreSetting) continue;
            settingsCopy[kvp.Key] = new List<SettingDefine>(kvp.Value);
        }
        return settingsCopy;
    }
    
    public Dictionary<string, List<SettingDefine>> GetSettings() {
        var settingsCopy = new Dictionary<string, List<SettingDefine>>();
        foreach (var kvp in _settings) {
            if (kvp.Key == CoreSetting) continue;
            settingsCopy[kvp.Key] = new List<SettingDefine>(kvp.Value);
        }
        return settingsCopy;
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
    }

    public void Reset() {
        instance = new SettingsManager();
        instance.ReloadCoreSettings();
        Dispose();
    }

    public void SaveCoreSettings() {
        var basePath = OS.HasFeature("editor") ? "res://" : OS.GetExecutablePath().GetBaseDir();
        if (!DirAccess.DirExistsAbsolute(basePath)) {
            throw new DirectoryNotFoundException($"Settings directory does not exist at path: {basePath}");
        }
        var settingsToArchive = new Dictionary<string, string>();
        // only save value
        foreach (var kvp in _settings[CoreSetting]) {
            settingsToArchive[kvp.Key] = kvp.Value;
        }
        var json = JsonSerializer.Serialize(settingsToArchive);
        var filePath = Path.Combine(basePath, SettingsFile);
        var fileHandle = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
        fileHandle.StoreBuffer(System.Text.Encoding.UTF8.GetBytes(json));
    }
    
    public void ReloadCoreSettings() {
        var basePath = OS.HasFeature("editor") ? "res://" : OS.GetExecutablePath().GetBaseDir();
        var filePath = Path.Combine(basePath, SettingsFile);
        if (!FileAccess.FileExists(filePath)) {
            _logger.LogWarning("Settings file does not exist at path: {Path}", filePath);
            return;
        }
        var fileHandle = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
        var data = fileHandle.GetBuffer((int)fileHandle.GetLength());
        var json = System.Text.Encoding.UTF8.GetString(data);
        if (string.IsNullOrWhiteSpace(json)) return;
        var settingsFromFile = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        if (settingsFromFile != null) {
            foreach (var kvp in settingsFromFile) {
                if (!_settings.ContainsKey(CoreSetting)) {
                    _settings[CoreSetting] = [];
                }
                var target = _settings[CoreSetting].FindIndex(s => s.Key == kvp.Key);
                if (target < 0) continue;
                var item = _settings[CoreSetting][target];
                item.Value = kvp.Value;
                _settings[CoreSetting][target] = item;
            }
        } else {
            _logger.LogWarning("Failed to deserialize settings from file: {Path}", filePath);
        }
    }

    public void Archive(Dictionary<string, byte[]> fileList) {
        var settingsToArchive = new Dictionary<string, List<SettingDefine>>(_settings);
        settingsToArchive.Remove(CoreSetting);
        var waitSerialize = new Dictionary<string, Dictionary<string, string>>();
        foreach (var kvp in settingsToArchive) {
            var serializedSettings = new Dictionary<string, string>();
            foreach (var setting in kvp.Value) {
                serializedSettings[setting.Key] = setting.Value;
            }
            waitSerialize[kvp.Key] = serializedSettings;
        }
        var json = JsonSerializer.Serialize(waitSerialize);
        fileList[SettingsFile] = System.Text.Encoding.UTF8.GetBytes(json);
    }
    
    public void Recover(Func<string, byte[]> getDataFunc) {
        if (getDataFunc(SettingsFile) is { } data) {
            var json = System.Text.Encoding.UTF8.GetString(data);
            var settingsFromFile = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
            if (settingsFromFile == null) return;
            foreach (var level1 in settingsFromFile) {
                if (!_settings.ContainsKey(level1.Key)) {
                    _settings[level1.Key] = [];
                }
                foreach (var level2 in level1.Value) {
                    var target = _settings[level1.Key].FindIndex(s => s.Key == level2.Key);
                    if (target < 0) continue;
                    var item = _settings[level1.Key][target];
                    item.Value = level2.Value;
                    _settings[level1.Key][target] = item;
                }
            }
        } else {
            Reset();
        }
    }
}