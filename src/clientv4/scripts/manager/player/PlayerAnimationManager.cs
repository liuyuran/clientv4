using System.Collections.Generic;
using game.scripts.utils;
using Godot;

namespace game.scripts.manager.player;

public partial class PlayerManager {
    private AnimationPlayer _animationPlayer;
    private readonly Dictionary<string, AnimationLibrary> _animationLibraries = new();

    public void InitializeAnimation() {
        _animationPlayer = new AnimationPlayer();
        var animationTree = new AnimationTree();
        _animationPlayer.AddChild(animationTree);
    }
    
    public void DeinitializeAnimation() {
        _animationPlayer.QueueFree();
        _animationPlayer = null;
        _animationLibraries.Clear();
    }
    
    public void RegistryAnimation(string libKey, string name, string path, bool autoLoop = false) {
        if (!_animationLibraries.TryGetValue(libKey, out var library)) {
            _animationLibraries.Add(libKey, new AnimationLibrary());
            library = _animationLibraries[libKey];
        }

        var animation = ResourceLoader.Load<Animation>($"file://{path}");
        animation.LoopMode = autoLoop ? Animation.LoopModeEnum.Linear : Animation.LoopModeEnum.None;
        library.AddAnimation(name, animation);
    }

    public void AttachAnimationNode(Node creatureNode) {
        var animationPlayer = _animationPlayer.Duplicate();
        creatureNode.AddChild(animationPlayer);
        animationPlayer.Name = "AnimationPlayer";
        animationPlayer.Owner = creatureNode;
    }
    
    public void DetachAnimationNode(Node creatureNode) {
        var animationPlayer = creatureNode.FindNodeByName<AnimationPlayer>("AnimationPlayer");
        animationPlayer.Stop();
        animationPlayer.QueueFree();
    }
}