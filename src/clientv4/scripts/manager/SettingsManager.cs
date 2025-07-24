using System;
using System.Collections.Generic;
using game.scripts.manager.archive;
using game.scripts.manager.reset;
using ModLoader.handler;

namespace game.scripts.manager;

public class SettingsManager : ISettingsManager, IDisposable, IReset, IArchive {
    public static SettingsManager instance { get; private set; } = new();
    private readonly Dictionary<string, List<SettingDefine>> _settings = new();

    private SettingsManager() {
        // load main settings
    }
    
    public void AddSetting(string module, SettingDefine setting) {
        if (!_settings.ContainsKey(module)) {
            _settings[module] = [];
        }
        _settings[module].Add(setting);
    }
    
    public List<SettingDefine> GetSettings(string module) {
        if (_settings.TryGetValue(module, out var settings)) {
            return settings;
        }
        return [];
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
    }
    
    public struct SettingDefine {
        public string Key;
        public object Config;
        public string Value;
        public string DefaultValue;
        public string Description;
        public Action<string> OnChange;
        public int Order;
    }

    public void Reset() {
        instance = new SettingsManager();
        Dispose();
    }

    public void Archive(Dictionary<string, byte[]> fileList) {
        // only control mod settings
    }
    
    public void Recover(Func<string, byte[]> getDataFunc) {
        // only control mod settings
    }
}