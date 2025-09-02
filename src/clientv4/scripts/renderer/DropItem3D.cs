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
using ModLoader.util;
using Vector3I = Godot.Vector3I;

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
        _area = this.GetParent<Node>().FindNodeByName<Area3D>("Area3D");
        _area.Transform = new Transform3D {
            Origin = new Vector3(-.25, -.25, -.25),
        };
        if (_area == null) {
            GD.PrintErr("DropItem3D: Area3D node not found");
            return;
        }
        var collisionShape = _area.GetParent<Node>().FindNodeByName<CollisionShape3D>("CollisionShape3D");
        if (collisionShape == null) {
            GD.PrintErr("DropItem3D: CollisionShape3D node not found");
            return;
        }
        var boxShape = new BoxShape3D();
        boxShape.Size = new Vector3(0.5f, 0.5f, 0.5f);
        collisionShape.Shape = boxShape;
        _area.BodyShapeEntered += AreaOnBodyShapeEntered;
    }

    public override void _ExitTree() {
        _area.BodyShapeEntered -= AreaOnBodyShapeEntered;
    }

    private void AreaOnBodyShapeEntered(Rid bodyRid, Node3D body, long bodyShapeIndex, long localShapeIndex) {
        _logger.Log(LogLevel.Debug, "DropItem3D: BodyShapeEntered {Body} {BodyShapeIndex} {LocalShapeIndex}", body.Name, bodyShapeIndex, localShapeIndex);
        var nodeName = body.Name.ToString();
        if (nodeName.StartsWith("Player_")) {
            var entityId = nodeName["Player_".Length..];
            var entity = GameNodeReference.World.GetEntityById(Convert.ToInt32(entityId));
            if (!entity.HasComponent<CPeer>()) return;
            var peer = entity.GetComponent<CPeer>();
            var player = PlayerManager.instance.GetPlayerByPeerId(peer.PeerId);
            if (player == null) return;
            InventoryManager.instance.AddItemToInventory(player.playerId, _itemId, _amount, _lootItemData);
            var playerPosition = player.position;
            var tween = new Tween();
            tween.TweenProperty(this, "transform/origin", playerPosition, 0.5f).SetTrans(Tween.TransitionType.Linear);
            tween.Play();
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
        AddCubeMesh(meshTool, _itemId, flags, ref baseIndex, Vector3I.Zero);
        var mesh = meshTool.Commit();
        var material = MaterialManager.instance.GetMaterial();
        mesh.SurfaceSetMaterial(0, material);
        Mesh = mesh;
        Scale = new Vector3(0.1, 0.1, 0.1);
    }
    
    private static void AddCubeMesh(SurfaceTool tool, ulong blockId, int directionFlag, ref int baseIndex, Vector3I point) {
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