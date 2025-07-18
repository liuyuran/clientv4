using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Microsoft.Extensions.Logging;
using ModLoader.logger;
using FileAccess = Godot.FileAccess;

namespace game.scripts.manager.archive;

public class ArchiveManager {
    private readonly ILogger _logger = LogManager.GetLogger<ArchiveManager>();
    public static ArchiveManager instance { get; private set; } = new();
    
    private const string SaveDirectory = "worlds";
    private string _currentSaveName = string.Empty;

    public void Create(string saveName) {
        var basePath = OS.HasFeature("editor") ? "res://" : OS.GetExecutablePath().GetBaseDir();
        var saveBasePath = Path.Combine(basePath, SaveDirectory);
        
        if (!DirAccess.DirExistsAbsolute(saveBasePath)) {
            DirAccess.MakeDirAbsolute(saveBasePath);
        }
        
        var savePath = Path.Combine(saveBasePath, saveName);
        if (DirAccess.DirExistsAbsolute(savePath)) {
            _logger.LogWarning("Save directory '{SaveName}' already exists at path: {Path}", saveName, savePath);
            return;
        }
        
        DirAccess.MakeDirAbsolute(savePath);
        _currentSaveName = saveName;
    }

    public List<ArchiveMeta> List() {
        var basePath = OS.HasFeature("editor") ? "res://" : OS.GetExecutablePath().GetBaseDir();
        var saveBasePath = Path.Combine(basePath, SaveDirectory);
        
        if (!DirAccess.DirExistsAbsolute(saveBasePath)) {
            _logger.LogWarning("Save directory '{SaveDirectory}' does not exist at path: {Path}", SaveDirectory, saveBasePath);
            return [];
        }
        
        var archiveFiles = DirAccess.GetDirectoriesAt(saveBasePath);
        return archiveFiles.Select(file => new ArchiveMeta { Name = Path.GetFileName(file) }).ToList();
    }
    
    public void Save() {
        var basePath = OS.HasFeature("editor") ? "res://" : OS.GetExecutablePath().GetBaseDir();
        var saveBasePath = Path.Combine(basePath, SaveDirectory);
        
        if (!DirAccess.DirExistsAbsolute(saveBasePath)) {
            _logger.LogWarning("Save directory '{SaveDirectory}' does not exist at path: {Path}", SaveDirectory, saveBasePath);
            return;
        }
        
        var archiveFiles = new Dictionary<string, byte[]>();
        var resetTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(IArchive).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);
        foreach (var type in resetTypes) {
            var archiveInstance = (IArchive)Activator.CreateInstance(type);
            archiveInstance?.Archive(archiveFiles);
        }
        foreach (var file in archiveFiles) {
            if (file.Key.Trim().Length == 0) continue;
            var filePath = Path.Combine(saveBasePath, _currentSaveName, file.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? string.Empty);
            var fileHandle = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
            fileHandle.StoreBuffer(file.Value);
        }
    }

    public void Load(string saveName) {
        _currentSaveName = saveName;
        var basePath = OS.HasFeature("editor") ? "res://" : OS.GetExecutablePath().GetBaseDir();
        var saveBasePath = Path.Combine(basePath, SaveDirectory);
        
        if (!DirAccess.DirExistsAbsolute(saveBasePath)) {
            _logger.LogWarning("Save directory '{SaveDirectory}' does not exist at path: {Path}", SaveDirectory, saveBasePath);
            return;
        }
        
        var resetTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(IArchive).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);
        foreach (var type in resetTypes) {
            var archiveInstance = (IArchive)Activator.CreateInstance(type);
            archiveInstance?.Recover(path => {
                var filePath = Path.Combine(saveBasePath, saveName, path);
                if (FileAccess.FileExists(filePath)) {
                    return FileAccess.GetFileAsBytes(filePath);
                }
                return null;
            });
        }
    }
    
    public struct ArchiveMeta {
        public string Name;
    }
}