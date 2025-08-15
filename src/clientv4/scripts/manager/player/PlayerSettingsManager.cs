using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using game.scripts.manager.player.settings;
using Godot;
using Microsoft.Extensions.Logging;
using ModLoader.logger;
using FileAccess = Godot.FileAccess;

namespace game.scripts.manager.player;

public class PlayerSettingsManager {
    private readonly ILogger _logger = LogManager.GetLogger<PlayerSettingsManager>();
    public static PlayerSettingsManager instance { get; private set; } = new();
    private const string SettingsFile = "player-settings.json";
    private PlayerSettings _settings;
    
    public struct PlayerSettings {
        public ActionBarSettings ActionBar;
    }
    
    private PlayerSettingsManager() {
        ReloadSettings();
    }

    public PlayerSettings GetSettings() {
        return _settings;
    }

    public void SetActionBar(ActionBarSettings setting) {
        _settings.ActionBar = setting;
        SaveSettings();
    }
    
    private void SaveSettings() {
        var basePath = OS.HasFeature("editor") ? "res://" : OS.GetExecutablePath().GetBaseDir();
        if (!DirAccess.DirExistsAbsolute(basePath)) {
            throw new DirectoryNotFoundException($"Settings directory does not exist at path: {basePath}");
        }
        var json = JsonSerializer.Serialize(_settings);
        var filePath = Path.Combine(basePath, SettingsFile);
        var fileHandle = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
        fileHandle.StoreBuffer(System.Text.Encoding.UTF8.GetBytes(json));
        fileHandle.Flush();
    }

    private void ReloadSettings() {
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
        _settings = JsonSerializer.Deserialize<PlayerSettings>(json);
    }
}