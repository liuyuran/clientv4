using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using Tomlyn;
using FileAccess = Godot.FileAccess;

namespace game.scripts.manager;

/// <summary>
/// resource pack support.
/// if someone wants to replace some texture or model, thou can create a resource pack that has higher priority.
/// every resource pack that has the same name has a priority, and if program cannot find a texture in a pack that has higher priority, then will find in the next pack.
/// </summary>
public class ResourcePackManager {
    public static ResourcePackManager instance { get; private set; } = new();
    
    private readonly Dictionary<string, List<ResourcePackInfo>> _resourcePackPaths = new();
    private const string ResourcePackDirectory = "ResourcePack";
    
    public void ScanResourcePacks() {
        var basePath = OS.HasFeature("editor") ? "res://" : OS.GetExecutablePath().GetBaseDir();
        var resourcePackPath = Path.Combine(basePath, ResourcePackDirectory);
        
        if (!DirAccess.DirExistsAbsolute(resourcePackPath)) return;
        
        foreach (var directory in DirAccess.GetDirectoriesAt(resourcePackPath)) {
            var metaPath = Path.Combine(resourcePackPath, directory, "meta.toml");
            if (!FileAccess.FileExists(metaPath)) continue;
            
            try {
                var metaContent = FileAccess.GetFileAsBytes(metaPath);
                var metadata = Toml.ToModel<ResourcePackMeta>(metaContent.GetStringFromUtf8()[1..]);
                if (metadata.name != null) {
                    if (!_resourcePackPaths.ContainsKey(metadata.name)) {
                        _resourcePackPaths[metadata.name] = [];
                    }
                    _resourcePackPaths[metadata.name].Add(new ResourcePackInfo {
                        displayName = metadata.displayName,
                        name = metadata.name,
                        priority = metadata.priority,
                        description = metadata.description,
                        path = Path.Combine(resourcePackPath, directory)
                    });
                }
            } catch (Exception e) {
                GD.PrintErr($"Failed to load resource pack metadata from {metaPath}: {e.Message}");
            }
        }
        
        // sort resource pack by priority desc
        foreach (var packName in _resourcePackPaths.Keys.ToList()) {
            _resourcePackPaths[packName] = _resourcePackPaths[packName]
                .OrderByDescending(info => info.priority)
                .ToList();
        }
    }
    
    public ResourcePackInfo[] GetAllResourcePacks() {
        return _resourcePackPaths.Values.SelectMany(p => p).OrderByDescending(info => info.priority).ToArray();
    }

    public string GetFileAbsolutePath(string uri) {
        var parts = uri.Split(":/", 2);
        if (parts.Length != 2) throw new ArgumentException("Invalid resource URI format");

        var packName = parts[0];
        var filePath = parts[1];

        if (!_resourcePackPaths.TryGetValue(packName, out var packPath)) {
            throw new FileNotFoundException($"Resource pack named '{packName}' not found");
        }

        foreach (var packInfo in packPath) {
            var fullPath = Path.Combine(packInfo.path, filePath);
            if (!FileAccess.FileExists(fullPath)) {
                continue;
            }

            return fullPath;
        }

        throw new FileNotFoundException($"File '{filePath}' not found in all resource pack named '{packName}'");
    }
    
    private class ResourcePackMeta {
        public string displayName { get; init; }
        public string name { get; init; }
        public ulong priority { get; init; }
        public string description { get; init; }
    }
    
    public class ResourcePackInfo {
        public string displayName { get; init; }
        public string name { get; init; }
        public ulong priority { get; init; }
        public string description { get; init; }
        public string path { get; init; }
    }
}