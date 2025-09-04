using System;
using System.Collections.Generic;
using game.scripts.manager;
using game.scripts.manager.blocks;
using game.scripts.manager.item;
using game.scripts.manager.player;
using game.scripts.server.ECSBridge.sync;
using game.scripts.utils;
using Godot;
using Microsoft.Extensions.Logging;
using ModLoader.item.composition;
using ModLoader.logger;
using ModLoader.util;

namespace game.scripts.renderer;

/// <summary>
/// an 3D piece for dropped block item, auto rotating
/// </summary>
public partial class DropItem3D: MeshInstance3D {
    private readonly ILogger _logger = LogManager.GetLogger<DropItem3D>();
    private ulong _itemId;
    private long _amount;
    private Dictionary<string, object> _lootItemData;
    private Area3D _area;
    private bool _needRender;
    private bool _needRotate;

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

    public void SetItem(ulong itemId, long amount, Dictionary<string, object> lootItemData) {
        _itemId = itemId;
        _amount = amount;
        _lootItemData = lootItemData;
        _needRender = true;
    }
    
    public override void _Process(double delta) {
        if (_needRotate) {
            // auto rotate when mesh has been built
            RotateY(Mathf.DegToRad(0.5));
        }
        if (!_needRender) return;
        UpdateMesh();
        _needRender = false;
        _needRotate = true;
    }

    private void UpdateMesh() {
        var meshTool = new SurfaceTool();
        meshTool.Begin(Mesh.PrimitiveType.Triangles);
        var baseIndex = 0;
        var flags = 0;
        flags |= 1 << (int)Direction.North;
        flags |= 1 << (int)Direction.South;
        flags |= 1 << (int)Direction.East;
        flags |= 1 << (int)Direction.West;
        flags |= 1 << (int)Direction.Up;
        flags |= 1 << (int)Direction.Down;
        var item = ItemManager.instance.GetItem(_itemId);
        var blockId = BlockManager.instance.GetBlockId(item.GetBlockName());
        // must make the cube center at origin, otherwise the rotation will be around with the (0,0,0) point
        AddCubeMesh(meshTool, blockId, flags, ref baseIndex, new Vector3(-0.5f, -0.5f, -0.5f));
        var mesh = meshTool.Commit();
        var material = MaterialManager.instance.GetMaterial();
        mesh.SurfaceSetMaterial(0, material);
        Mesh = mesh;
        Scale = new Vector3(0.3, 0.3, 0.3);
    }
    
    private static void AddCubeMesh(SurfaceTool tool, ulong blockId, int directionFlag, ref int baseIndex, Vector3 point) {
        if (directionFlag == 0) {
            return;
        }
        if ((directionFlag & (1 << (int)Direction.South)) > 0) {
            var uv = GetUV(blockId, Direction.South);
            tool.SetNormal(new Vector3(0, 0, 1));
            tool.SetUV(uv[0]);
            tool.AddVertex(new Vector3(0, 1, 1) + point);
            tool.SetUV(uv[1]);
            tool.AddVertex(new Vector3(1, 1, 1) + point);
            tool.SetUV(uv[2]);
            tool.AddVertex(new Vector3(1, 0, 1) + point);
            tool.SetUV(uv[3]);
            tool.AddVertex(new Vector3(0, 0, 1) + point);
            AddIndex(tool, baseIndex);
            baseIndex += 4;
        }
        if ((directionFlag & (1 << (int)Direction.North)) > 0) {
            var uv = GetUV(blockId, Direction.North);
            tool.SetNormal(new Vector3(0, 0, -1));
            tool.SetUV(uv[0]);
            tool.AddVertex(new Vector3(1, 1, 0) + point);
            tool.SetUV(uv[1]);
            tool.AddVertex(new Vector3(0, 1, 0) + point);
            tool.SetUV(uv[2]);
            tool.AddVertex(new Vector3(0, 0, 0) + point);
            tool.SetUV(uv[3]);
            tool.AddVertex(new Vector3(1, 0, 0) + point);
            AddIndex(tool, baseIndex);
            baseIndex += 4;
        }
        if ((directionFlag & (1 << (int)Direction.East)) > 0) {
            var uv = GetUV(blockId, Direction.East);
            tool.SetNormal(new Vector3(1, 0, 0));
            tool.SetUV(uv[0]);
            tool.AddVertex(new Vector3(1, 1, 1) + point);
            tool.SetUV(uv[1]);
            tool.AddVertex(new Vector3(1, 1, 0) + point);
            tool.SetUV(uv[2]);
            tool.AddVertex(new Vector3(1, 0, 0) + point);
            tool.SetUV(uv[3]);
            tool.AddVertex(new Vector3(1, 0, 1) + point);
            AddIndex(tool, baseIndex);
            baseIndex += 4;
        }
        if ((directionFlag & (1 << (int)Direction.West)) > 0) {
            var uv = GetUV(blockId, Direction.West);
            tool.SetNormal(new Vector3(-1, 0, 0));
            tool.SetUV(uv[0]);
            tool.AddVertex(new Vector3(0, 1, 0) + point);
            tool.SetUV(uv[1]);
            tool.AddVertex(new Vector3(0, 1, 1) + point);
            tool.SetUV(uv[2]);
            tool.AddVertex(new Vector3(0, 0, 1) + point);
            tool.SetUV(uv[3]);
            tool.AddVertex(new Vector3(0, 0, 0) + point);
            AddIndex(tool, baseIndex);
            baseIndex += 4;
        }
        if ((directionFlag & (1 << (int)Direction.Up)) > 0) {
            var uv = GetUV(blockId, Direction.Up);
            tool.SetNormal(new Vector3(0, 1, 0));
            tool.SetUV(uv[0]);
            tool.AddVertex(new Vector3(0, 1, 0) + point);
            tool.SetUV(uv[1]);
            tool.AddVertex(new Vector3(1, 1, 0) + point);
            tool.SetUV(uv[2]);
            tool.AddVertex(new Vector3(1, 1, 1) + point);
            tool.SetUV(uv[3]);
            tool.AddVertex(new Vector3(0, 1, 1) + point);
            AddIndex(tool, baseIndex);
            baseIndex += 4;
        }
        if ((directionFlag & (1 << (int)Direction.Down)) > 0) {
            var uv = GetUV(blockId, Direction.Down);
            tool.SetNormal(new Vector3(0, -1, 0));
            tool.SetUV(uv[0]);
            tool.AddVertex(new Vector3(1, 0, 0) + point);
            tool.SetUV(uv[1]);
            tool.AddVertex(new Vector3(0, 0, 0) + point);
            tool.SetUV(uv[2]);
            tool.AddVertex(new Vector3(0, 0, 1) + point);
            tool.SetUV(uv[3]);
            tool.AddVertex(new Vector3(1, 0, 1) + point);
            AddIndex(tool, baseIndex);
            baseIndex += 4;
        }
    }

    private static void AddIndex(SurfaceTool tool, int baseIndex) {
        tool.AddIndex(baseIndex + 3);
        tool.AddIndex(baseIndex);
        tool.AddIndex(baseIndex + 1);
        tool.AddIndex(baseIndex + 3);
        tool.AddIndex(baseIndex + 1);
        tool.AddIndex(baseIndex + 2);
    }
    
    private static Vector2[] GetUV(ulong blockId, Direction direction) {
        return MaterialManager.instance.GetUVs(blockId, direction);
    }
}