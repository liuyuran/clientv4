using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using DotnetNoise;
using game.scripts.manager.reset;
using game.scripts.utils;
using Godot;
using Microsoft.Extensions.Logging;
using ModLoader;
using ModLoader.handler;
using ModLoader.logger;
using Tomlyn;
using FileAccess = Godot.FileAccess;

namespace game.scripts.manager.mod;

public class ModManager: IReset, IDisposable {
    private readonly ILogger _logger = LogManager.GetLogger<ModManager>();
    public static ModManager instance { get; private set; } = new();
    private const string ModDirectory = "Mods";
    private static bool _loaded;
    private readonly Dictionary<string, IMod> _modInstances = new();
    private readonly Dictionary<string, ModMeta> _modMetas = new();
    private readonly HashSet<string> _activeMods = [];
    private readonly Dictionary<string, Assembly> _loadedAssembly = [];
    private readonly List<Assembly> _extraAssemblies = [
        typeof(FastNoise).Assembly
    ];
    private readonly IModHandler _modHandler = new StandardModHandler();

    private ModManager() {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            var name = assembly.GetName().Name;
            if (name != null) _loadedAssembly.Add(name, assembly);
        }

        foreach (var extra in _extraAssemblies) {
            var name = extra.GetName().Name;
            if (name != null) _loadedAssembly.TryAdd(name, extra);
        }
        if (_loaded) return;
        AppDomain.CurrentDomain.AssemblyResolve += OnCurrentDomainOnAssemblyResolve;
        AssemblyLoadContext.Default.Resolving += OnDefaultOnResolving;
        _loaded = true;
    }

    private Assembly OnDefaultOnResolving(AssemblyLoadContext _, AssemblyName assemblyName) {
        if (assemblyName.Name != null && _loadedAssembly.TryGetValue(assemblyName.Name, out var assembly)) {
            return assembly;
        }

        return null;
    }

    private Assembly OnCurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args) {
        var assemblyName = new AssemblyName(args.Name);
        if (assemblyName.Name != null && _loadedAssembly.TryGetValue(assemblyName.Name, out var assembly)) {
            return assembly;
        }
        return null;
    }

    public void ScanModPacks() {
        var basePath = OS.HasFeature("editor") ? "res://" : OS.GetExecutablePath().GetBaseDir();
        var modPath = Path.Combine(basePath, ModDirectory);
        if (!DirAccess.DirExistsAbsolute(modPath)) {
            _logger.LogError("Mod directory '{ModDirectory}' does not exist at path: {Path}", ModDirectory, modPath);
            return;
        }

        var targetInterface = typeof(IMod);
        foreach (var directory in DirAccess.GetDirectoriesAt(modPath)) {
            if (directory.StartsWith('.')) continue;
            if (directory.StartsWith('~')) continue;
            var metaPath = Path.Combine(modPath, directory, "meta.toml");
            if (!FileAccess.FileExists(metaPath)) {
                _logger.LogWarning("Mod metadata file 'meta.toml' not found in directory: {Directory}", directory);
                continue;
            }

            try {
                var metaContent = FileUtil.RemoveBom(FileAccess.GetFileAsBytes(metaPath));
                var metadata = Toml.ToModel<ModMeta>(metaContent);
                if (metadata.name == null) continue;
                if (_modInstances.ContainsKey(metadata.name)) {
                    _logger.LogWarning("Mod with name '{Name}' already loaded, skipping duplicate.", metadata.name);
                    continue;
                }

                var dllPath = Path.Combine(modPath, directory, metadata.lib);
                if (!FileAccess.FileExists(dllPath)) {
                    _logger.LogWarning("Mod library file '{Lib}' not found in directory: {Directory}", metadata.lib, directory);
                    continue;
                }

                // load dll file as IMod
                var assembly = Assembly.Load(FileAccess.GetFileAsBytes(dllPath));
                Type targetType = null;
                foreach (var type in assembly.GetTypes()) {
                    if (!type.IsClass || type.IsAbstract || type.GetInterfaces().All(item => item != targetInterface)) continue;
                    targetType = type;
                    break;
                }

                if (targetType == null) {
                    _logger.LogWarning("No valid mod class found in assembly: {Assembly}", dllPath);
                    continue;
                }

                var modInstance = (IMod)Activator.CreateInstance(targetType);
                _modMetas.Add(metadata.name, metadata);
                _modInstances.Add(metadata.name, modInstance);
            } catch (Exception e) {
                _logger.LogError("Failed to load resource pack metadata from {s}: {eMessage}", metaPath, e.Message);
            }
        }
    }

    public ModMeta[] GetAllMods() {
        return _modMetas.Values.ToArray();
    }

    public IMod GetModInstance(string name) {
        if (_modInstances.TryGetValue(name, out var modInstance)) {
            return modInstance;
        }

        _logger.LogWarning("Mod with name '{Name}' not found.", name);
        return null;
    }

    public void ActivateMod(string name) {
        if (_activeMods.Contains(name)) {
            _logger.LogWarning("Mod '{Name}' is already active.", name);
            return;
        }

        if (!_modInstances.TryGetValue(name, out var modInstance)) {
            _logger.LogError("Mod with name '{Name}' not found.", name);
            return;
        }

        modInstance.OnLoad(_modHandler);
        _activeMods.Add(name);
        _logger.LogInformation("Activated mod: {Name}", name);
    }

    public void DeactivateMod(string name) {
        if (!_activeMods.Contains(name)) {
            _logger.LogWarning("Mod '{Name}' is not active.", name);
            return;
        }

        if (!_modInstances.TryGetValue(name, out var modInstance)) {
            _logger.LogError("Mod with name '{Name}' not found.", name);
            return;
        }

        modInstance.OnUnload();
        _activeMods.Remove(name);
        _logger.LogInformation("Deactivated mod: {Name}", name);
    }

    public void OnStartGame() {
        foreach (var modName in _activeMods) {
            if (_modInstances.TryGetValue(modName, out var modInstance)) {
                modInstance.OnGameStart();
                _logger.LogInformation("Started game for mod: {Name}", modName);
            } else {
                _logger.LogWarning("Mod instance for '{Name}' not found during game start.", modName);
            }
        }
    }

    public void OnStopGame() {
        foreach (var modName in _activeMods) {
            if (_modInstances.TryGetValue(modName, out var modInstance)) {
                modInstance.OnGameStop();
                _logger.LogInformation("Stopped game for mod: {Name}", modName);
            } else {
                _logger.LogWarning("Mod instance for '{Name}' not found during game stop.", modName);
            }
        }
    }

    public class ModMeta {
        public string displayName { get; init; }
        public string name { get; init; }
        public string lib { get; init; }
        public ulong priority { get; init; }
        public string description { get; init; }
    }

    public void Reset() {
        instance = new ModManager();
        Dispose();
    }
    public void Dispose() {
        _modInstances.Clear();
        _modMetas.Clear();
        _activeMods.Clear();
        GC.SuppressFinalize(this);
    }
}