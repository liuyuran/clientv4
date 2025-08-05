using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Godot;
using Microsoft.Extensions.Logging;
using ModLoader.archive;
using ModLoader.logger;
using FileAccess = Godot.FileAccess;

namespace game.scripts.manager.archive;

public class ArchiveManager {
    private readonly ILogger _logger = LogManager.GetLogger<ArchiveManager>();
    public static ArchiveManager instance { get; private set; } = new();

    private const string SaveDirectory = "Worlds";
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
        Save();
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
            using var fields = type.GetRuntimeFields().GetEnumerator();
            while (fields.MoveNext()) {
                var field = fields.Current;
                if (field == null) continue;
                if (field.IsStatic && field.Name.Contains("<instance>", StringComparison.OrdinalIgnoreCase)) {
                    // 如果有名为instance的静态字段，则直接获取该字段的值
                    var fieldValue = field.GetValue(null);
                    if (fieldValue is IArchive archiveInstance) {
                        archiveInstance.Archive(archiveFiles);
                    }
                }
            }
        }

        foreach (var file in archiveFiles) {
            if (file.Key.Trim().Length == 0) continue;
            var filePath = Path.Combine(saveBasePath, _currentSaveName, file.Key);
            DirAccess.MakeDirAbsolute(Path.GetDirectoryName(filePath) ?? string.Empty);
            var extension = Path.GetExtension(filePath);
            switch (extension) {
                case ".dat": {
                    var fileHandle = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
                    fileHandle.StoreBuffer(file.Value);
                    fileHandle.Flush();
                    break;
                }
                case ".log": {
                    using var fileStream = new FileStream(filePath, FileMode.Append);
                    fileStream.Write(file.Value, 0, file.Value.Length);
                    fileStream.Flush();
                    break;
                }
                default: {
                    var fileHandle = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
                    fileHandle.StoreBuffer(file.Value);
                    fileHandle.Flush();
                    break;
                }
            }
        }
    }

    public void RecoverData() {
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
            using var fields = type.GetRuntimeFields().GetEnumerator();
            while (fields.MoveNext()) {
                var field = fields.Current;
                if (field == null) continue;
                if (field.IsStatic && field.Name.Contains("<instance>", StringComparison.OrdinalIgnoreCase)) {
                    // 如果有名为instance的静态字段，则直接获取该字段的值
                    var fieldValue = field.GetValue(null);
                    if (fieldValue is IArchive archiveInstance) {
                        archiveInstance.Recover(GetFileAsBytesFromCurrentArchive);
                    }
                }
            }
        }
    }
    
    public void SaveFileAsBytesToCurrentArchive(string relativePath, byte[] data) {
        var basePath = OS.HasFeature("editor") ? "res://" : OS.GetExecutablePath().GetBaseDir();
        var saveBasePath = Path.Combine(basePath, SaveDirectory);
        var filePath = Path.Combine(saveBasePath, _currentSaveName, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? string.Empty);
        var fileHandle = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
        fileHandle.StoreBuffer(data);
        fileHandle.Flush();
    }
    
    public byte[] GetFileAsBytesFromCurrentArchive(string relativePath) {
        var basePath = OS.HasFeature("editor") ? "res://" : OS.GetExecutablePath().GetBaseDir();
        var saveBasePath = Path.Combine(basePath, SaveDirectory);
        var filePath = Path.Combine(saveBasePath, _currentSaveName, relativePath);
        if (FileAccess.FileExists(filePath)) {
            return FileAccess.GetFileAsBytes(filePath);
        }

        return null;
    }

    public void Load(string saveName) {
        _currentSaveName = saveName;
    }

    public struct ArchiveMeta {
        public string Name;
    }
}