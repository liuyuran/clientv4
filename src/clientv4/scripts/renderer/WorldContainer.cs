using System.Collections.Concurrent;
using System.Globalization;
using game.scripts.manager;
using game.scripts.manager.map;
using game.scripts.manager.mod;
using game.scripts.manager.player;
using game.scripts.utils;
using Godot;
using Microsoft.Extensions.Logging;
using ModLoader.logger;

namespace game.scripts.renderer;

public partial class WorldContainer: Control {
    private readonly ILogger _logger = LogManager.GetLogger<WorldContainer>();
    [Export] private PackedScene _worldPrototype;
    [Export] private PackedScene _UIPrototype;
    private readonly ConcurrentDictionary<ulong, SubViewport> _subViewports = new();
    private ulong _currentWorldId;

    public override void _Ready() {
        DateUtil.goDotMode = true;
        RedirectLogger.WriteLine = (level, s) => {
            GD.Print(string.Format(CultureInfo.CurrentCulture, "[{0}] {1}", level, s));
        };
        PlayerManager.instance.InitializeAnimation();
        ResourcePackManager.instance.ScanResourcePacks();
        ModManager.instance.ScanModPacks();
        MaterialManager.instance.GenerateMaterials();
        MapManager.instance.OnBlockChanged += OnInstanceOnOnBlockChanged;
    }

    private void OnInstanceOnOnBlockChanged(ulong worldId, Vector3 position, ulong blockId, Direction direction) {
        if (_subViewports.ContainsKey(worldId)) return;
        CreateSubViewport(worldId);
    }

    public override void _Process(double delta) {
        if (PlatformUtil.isNetworkMaster) {
            var players = PlayerManager.instance.GetAllPlayers();
            foreach (var playerInfo in players) {
                if (playerInfo == null) continue;
                if (_subViewports.ContainsKey(playerInfo.worldId)) continue;
                CreateSubViewport(playerInfo.worldId);
            }
        } else {
            var currentPeerId = Multiplayer.MultiplayerPeer.GetUniqueId();
            var players = PlayerManager.instance.GetAllPlayers();
            foreach (var playerInfo in players) {
                if (playerInfo == null) continue;
                if (playerInfo.peerId != currentPeerId) continue;
                if (_subViewports.ContainsKey(playerInfo.worldId)) continue;
                CreateSubViewport(playerInfo.worldId);
            }
        }
    }

    public override void _ExitTree() {
        PlayerManager.instance.DeinitializeAnimation();
        MapManager.instance.OnBlockChanged -= OnInstanceOnOnBlockChanged;
    }
    
    public void SetCurrentWorld(ulong targetWorldId) {
        foreach (var (worldId, viewport) in _subViewports) {
            ((SubViewportContainer)viewport.GetParent()).Visible = worldId == targetWorldId;
        }
        _currentWorldId = targetWorldId;
        _logger.LogDebug("Set current world to {WorldId}", targetWorldId);
    }
    
    public SubViewport GetCurrentSubViewport() {
        if (_subViewports.TryGetValue(_currentWorldId, out var viewport)) {
            return viewport;
        }
        CreateSubViewport(_currentWorldId);
        return _subViewports[_currentWorldId];
    }

    private void CreateSubViewport(ulong worldId) {
        if (_subViewports.ContainsKey(worldId)) return;
        var viewportContainer = _worldPrototype.Instantiate<SubViewportContainer>();
        AddChild(viewportContainer);
        viewportContainer.Name = $"ViewportContainer_{worldId}";
        var subViewport = viewportContainer.GetNode<SubViewport>("world");
        subViewport.Name = $"World_{worldId}";
        var world = new WorldRender(worldId);
        subViewport.AddChild(world);
        world.InitRender();
        var light = new DirectionalLight3D();
        subViewport.AddChild(light);
        light.LightEnergy = 1.0f;
        light.LightColor = new Color(1.0f, 0.956f, 0.839f);
        light.RotationDegrees = new Vector3(-45, -45, 0);
        light.GlobalPosition = new Vector3(0, 5, 2);
        var camera = new Camera3D();
        subViewport.AddChild(camera);
        camera.Name = "MainCamera";
        camera.MakeCurrent();
        camera.GlobalPosition = new Vector3(0, 2, 2);
        var ui = _UIPrototype.Instantiate<CanvasLayer>();
        subViewport.AddChild(ui);
        _subViewports[worldId] = subViewport;
        _logger.LogDebug("Created sub viewport for world {WorldId}", worldId);
    }
}