using game.scripts.manager;
using game.scripts.manager.item.composition;
using game.scripts.utils;
using Godot;

namespace game.scripts.renderer;

public partial class DropItem2D: MeshInstance3D {
    private ulong _itemId;
    private bool _needRender;
    private bool _needRotate;
    
    public void SetItemId(ulong itemId) {
        _itemId = itemId;
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
        var item = ItemManager.instance.GetItem(_itemId);
        AddCubeMesh(meshTool, item.GetBlockId(), flags, ref baseIndex);
        var mesh = meshTool.Commit();
        var material = MaterialManager.instance.GetItemMaterial();
        mesh.SurfaceSetMaterial(0, material);
        Mesh = mesh;
        Scale = new Vector3(0.1, 0.1, 0.1);
    }
    
    private static void AddCubeMesh(SurfaceTool tool, ulong blockId, int directionFlag, ref int baseIndex) {
        if (directionFlag == 0) {
            return;
        }
        if ((directionFlag & (1 << (int)Direction.South)) > 0) {
            var uv = GetUV(blockId, Direction.South);
            tool.SetNormal(new Vector3(0, 0, 0.01));
            tool.SetUV(uv[0]);
            tool.AddVertex(new Vector3(0, 1, 0.01));
            tool.SetUV(uv[1]);
            tool.AddVertex(new Vector3(1, 1, 0.01));
            tool.SetUV(uv[2]);
            tool.AddVertex(new Vector3(1, 0, 0.01));
            tool.SetUV(uv[3]);
            tool.AddVertex(new Vector3(0, 0, 0.01));
            AddIndex(tool, baseIndex);
            baseIndex += 4;
        }
        if ((directionFlag & (1 << (int)Direction.North)) > 0) {
            var uv = GetUV(blockId, Direction.North);
            tool.SetNormal(new Vector3(0, 0, -1));
            tool.SetUV(uv[0]);
            tool.AddVertex(new Vector3(1, 1, 0));
            tool.SetUV(uv[1]);
            tool.AddVertex(new Vector3(0, 1, 0));
            tool.SetUV(uv[2]);
            tool.AddVertex(new Vector3(0, 0, 0));
            tool.SetUV(uv[3]);
            tool.AddVertex(new Vector3(1, 0, 0));
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
    
    private static Vector2[] GetUV(ulong itemId, Direction direction) {
        return MaterialManager.instance.GetItemUVs(itemId, direction);
    }
}