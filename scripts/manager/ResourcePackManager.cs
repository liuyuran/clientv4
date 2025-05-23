using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

namespace game.scripts.manager;

public class ResourcePackManager {
    public static ResourcePackManager instance { get; private set; } = new();
    
    private readonly Dictionary<string, string> _resourcePackPaths = new();
    private const string ResourcePackDirectory = "ResourcePack";
    
    public void ScanResourcePacks() {
        var basePath = @"D:\Game Dev\Projects\clientv4";//OS.GetExecutablePath().GetBaseDir();
        var resourcePackPath = Path.Combine(basePath, ResourcePackDirectory);
        
        if (!Directory.Exists(resourcePackPath)) return;
        
        foreach (var directory in Directory.GetDirectories(resourcePackPath)) {
            var metaPath = Path.Combine(directory, "meta.json");
            if (!File.Exists(metaPath)) continue;
            
            try {
                var metaContent = File.ReadAllText(metaPath);
                var metadata = JsonSerializer.Deserialize<ResourcePackMeta>(metaContent);
                if (metadata?.name != null) {
                    _resourcePackPaths[metadata.name] = directory;
                }
            } catch (Exception e) {
                GD.PrintErr($"Failed to load resource pack metadata from {metaPath}: {e.Message}");
            }
        }
    }
    
    public string GetFileAbsolutePath(string uri) {
        var parts = uri.Split(":/", 2);
        if (parts.Length != 2) throw new ArgumentException("Invalid resource URI format");
        
        var packName = parts[0];
        var filePath = parts[1];
        
        if (!_resourcePackPaths.TryGetValue(packName, out var packPath)) {
            throw new FileNotFoundException($"Resource pack '{packName}' not found");
        }
        
        var fullPath = Path.Combine(packPath, filePath);
        if (!File.Exists(fullPath)) {
            throw new FileNotFoundException($"File '{filePath}' not found in resource pack '{packName}'");
        }
        
        return fullPath;
    }
    
    private class ResourcePackMeta {
        public string name { get; init; }
    }
}