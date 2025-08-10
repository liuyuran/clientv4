using System;
using System.Collections.Generic;
using System.IO;
using game.scripts.config;
using game.scripts.manager.mod;
using game.scripts.manager.reset;
using game.scripts.utils;
using Godot;
using ModLoader.scene;
using FileAccess = Godot.FileAccess;

namespace game.scripts.manager.scene;

public class SceneManager : IReset, ISceneManager, IDisposable {
    public static SceneManager instance { get; private set; } = new();
    private readonly Dictionary<string, PackedScene> _scenes = new();
    private readonly Stack<Control> _modalStack = [];

    private PackedScene GetScene(string mod, string path) {
        var modPath = ModManager.instance.GetModPath(mod);
        var folder = DirAccess.Open(modPath);
        var filePath = Path.Combine(modPath, path);
        if (_scenes.TryGetValue(filePath, out var scene)) {
            return scene;
        }

        if (!FileAccess.FileExists(filePath)) return null;
        var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
        if (file == null) return null;
        folder.Copy(filePath, "user://load-scene-cache.tscn");
        var loadedScene = ResourceLoader.Load<PackedScene>("user://load-scene-cache.tscn");
        if (loadedScene == null) return null;
        _scenes[filePath] = loadedScene;
        return loadedScene;
    }

    private Control Instantiate(string mod, string path) {
        var node = GetScene(mod, path)?.Instantiate();
        if (node == null) return null;
        node.Name = Path.GetFileNameWithoutExtension(path);
        if (node is Control control) {
            return control;
        }

        GD.PrintErr($"Scene {path} is not a Control or Node3D.");
        return null;
    }

    public void OpenSceneModal(string mod, string path) {
        var modal = Instantiate(mod, path);
        if (modal == null) return;
        GameNodeReference.UI.AddChild(modal);
        _modalStack.Push(modal);
    }
    
    public void OpenSceneModal(string path) {
        var modal = ResourceLoader.Load<PackedScene>(path)?.Instantiate<Control>();
        if (modal == null) return;
        GameNodeReference.UI.AddChild(modal);
        modal.MouseFilter = Control.MouseFilterEnum.Pass;
        _modalStack.Push(modal);
    }

    public bool TryCloseSceneModal() {
        if (_modalStack.Count == 0) return false;
        var topModal = _modalStack.Pop();
        topModal.QueueFree();
        return true;
    }
    
    public void Reset() {
        instance = new SceneManager();
        Dispose();
    }

    public void Dispose() {
        foreach (var scene in _scenes.Values) {
            scene.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}