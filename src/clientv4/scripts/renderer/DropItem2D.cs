using System;
using System.Collections.Generic;
using game.scripts.manager;
using game.scripts.manager.item;
using game.scripts.manager.player;
using game.scripts.server.ECSBridge.sync;
using game.scripts.utils;
using Godot;
using Microsoft.Extensions.Logging;
using ModLoader.logger;

namespace game.scripts.renderer;

/// <summary>
/// an 2D piece for dropped item, auto rotating
/// </summary>
public partial class DropItem2D: Sprite3D {
    private readonly ILogger _logger = LogManager.GetLogger<DropItem2D>();
    private ulong _itemId;
    private long _amount;
    private Dictionary<string, object> _lootItemData;
    private Area3D _area;
    private bool _needRender;
    private bool _needRotate;
    
    public void SetItem(ulong itemId, long amount, Dictionary<string, object> lootItemData) {
        _itemId = itemId;
        _amount = amount;
        _lootItemData = lootItemData;
        _needRender = true;
    }
    
    public override void _Ready() {
        _area = this.GetParent<Node>().FindNodeByName<Area3D>("ColliderArea");
        _area.BodyShapeEntered += AreaOnBodyShapeEntered;
    }

    public override void _ExitTree() {
        _area.BodyShapeEntered -= AreaOnBodyShapeEntered;
    }

    /// <summary>
    /// it should be active when a pickup area that attached on player object starting include the cube area
    /// </summary>
    private void AreaOnBodyShapeEntered(Rid bodyRid, Node3D body, long bodyShapeIndex, long localShapeIndex) {
        var nodeName = body.Name.ToString();
        if (nodeName.StartsWith("Player_Pickup_")) {
            var entityId = nodeName["Player_Pickup_".Length..];
            var entity = GameNodeReference.World.GetEntityById(Convert.ToInt32(entityId));
            if (!entity.HasComponent<CPeer>()) return;
            var peer = entity.GetComponent<CPeer>();
            var player = PlayerManager.instance.GetPlayerByPeerId(peer.PeerId);
            if (player == null) return;
            InventoryManager.instance.AddItemToInventory(player.playerId, _itemId, _amount, _lootItemData);
            _logger.LogDebug("player {nickname} picked up item {id}, amount: {amount}", player.nickname, _itemId, _amount);
            var playerPosition = player.position;
            var tween = CreateTween();
            tween.TweenProperty(this, "global_position", playerPosition, .25f).SetTrans(Tween.TransitionType.Linear);
            tween.TweenCallback(Callable.From(QueueFree));
        }
    }
    
    public override void _Process(double delta) {
        if (_needRotate) {
            RotateY(Mathf.DegToRad(0.5));
        }
        if (!_needRender) return;
        UpdateMesh();
        _needRender = false;
        _needRotate = true;
    }

    private void UpdateMesh() {
        Texture = MaterialManager.instance.GetItemTexture(_itemId);
        var material = MaterialManager.instance.GetItemObjectMaterial();
        material.SetShaderParameter("albedo_texture", Texture);
        var uv = MaterialManager.instance.GetItemUVs(_itemId);
        var rect = new Vector4(uv[0].X, uv[0].Y, uv[2].X, uv[2].Y);
        material.SetShaderParameter("atlas_rect", rect);
        MaterialOverride = material;
        Scale = new Vector3(0.3, 0.3, 0.3);
    }
}