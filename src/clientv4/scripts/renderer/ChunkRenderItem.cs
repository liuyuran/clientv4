using game.scripts.manager;
using game.scripts.manager.blocks;
using game.scripts.manager.map;
using Godot;
using ModLoader.config;
using ModLoader.map.util;
using ModLoader.util;
using Vector3I = Godot.Vector3I;

namespace game.scripts.renderer;

public partial class ChunkRenderItem : MeshInstance3D {
    private BlockData[][][] _blockData;
    private Vector3I _chunkPosition;
    private ArrayMesh _readyMesh;
    private ArrayMesh _colliderMesh;
    private bool _isDirty;

    public override void _Process(double delta) {
        if (_isDirty) {
            UpdateMesh();
            _isDirty = false;
        }

        if (_readyMesh == null || Mesh == _readyMesh || _isDirty) return;
        Mesh = _readyMesh;
        if (_readyMesh._Surfaces.Count > 0) {
            UpdateCollider();
        }
        _readyMesh = null;
    }

    public void InitData(Vector3I chunkPosition, BlockData[][][] blockData) {
        _blockData = blockData;
        Name = $"Chunk_{chunkPosition.X}_{chunkPosition.Y}_{chunkPosition.Z}";
        _chunkPosition = chunkPosition;
        UpdateMesh();
        Mesh = _readyMesh;
        if (_readyMesh._Surfaces.Count > 0) {
            UpdateCollider();
        }
        _readyMesh = null;
    }

    [Rpc(CallLocal = true)]
    private void SetBlock(Vector3I pos, ulong blockId, int directionInt) {
        var direction = (Direction)directionInt;
        if (!IsValidPositionInChunk(pos)) {
            GD.PrintErr($"Invalid position: {pos}");
            return;
        }

        var blockData = GetBlockData(pos);
        if (blockData != null) {
            var data = new BlockData { BlockId = blockId, Direction = direction };
            _blockData[pos.X][pos.Y][pos.Z] = data;
            _isDirty = true;
        } else {
            GD.PrintErr($"Block data not found at position: {pos}");
        }
    }

    private void DestroyBlock(Vector3I pos) {
        SetBlock(pos, 0, (int)Direction.None);
    }

    private void PlaceBlock(Vector3I pos, ulong blockId, Direction direction) {
        SetBlock(pos, blockId, (int)direction);
    }
    
    private static bool IsValidPositionInChunk(Vector3I pos) {
        return pos.X >= 0 && pos.X < Config.ChunkSize &&
               pos.Y >= 0 && pos.Y < Config.ChunkSize &&
               pos.Z >= 0 && pos.Z < Config.ChunkSize;
    }

    private BlockData? GetBlockData(Vector3I pos) {
        if (!IsValidPositionInChunk(pos)) {
            return null;
        }
        return _blockData[pos.X][pos.Y][pos.Z];
    }
    
    private bool ShouldBeVisible(Vector3I pos, Direction direction) {
        if (!IsValidPositionInChunk(pos)) {
            return false;
        }

        var offset = new Vector3I(0, 0, 0);
        switch (direction) {
            case Direction.North:
                offset = new Vector3I(0, 0, -1);
                break;
            case Direction.South:
                offset = new Vector3I(0, 0, 1);
                break;
            case Direction.East:
                offset = new Vector3I(1, 0, 0);
                break;
            case Direction.West:
                offset = new Vector3I(-1, 0, 0);
                break;
            case Direction.Up:
                offset = new Vector3I(0, 1, 0);
                break;
            case Direction.Down:
                offset = new Vector3I(0, -1, 0);
                break;
            case Direction.None:
            default:
                break;
        }
        var neighborPos = pos + offset;
        if (!IsValidPositionInChunk(neighborPos)) {
            var blockId = MapManager.instance.GetBlockIdByPosition(_chunkPosition * Config.ChunkSize + neighborPos);
            return blockId switch {
                null or 0 => true,
                _ => BlockManager.instance.GetBlock(blockId.Value).transparent
            };
        }
        var neighborBlockData = GetBlockData(neighborPos);
        if (neighborBlockData == null) {
            return true;
        }

        if (neighborBlockData.Value.BlockId == 0) return true;
        var block = BlockManager.instance.GetBlock(neighborBlockData.Value.BlockId);
        return block.transparent;
    }
    
    private bool ShouldBeRender(Vector3I pos, Direction direction) {
        if (!IsValidPositionInChunk(pos)) {
            return false;
        }

        var offset = new Vector3I(0, 0, 0);
        switch (direction) {
            case Direction.North:
                offset = new Vector3I(0, 0, -1);
                break;
            case Direction.South:
                offset = new Vector3I(0, 0, 1);
                break;
            case Direction.East:
                offset = new Vector3I(1, 0, 0);
                break;
            case Direction.West:
                offset = new Vector3I(-1, 0, 0);
                break;
            case Direction.Up:
                offset = new Vector3I(0, 1, 0);
                break;
            case Direction.Down:
                offset = new Vector3I(0, -1, 0);
                break;
            case Direction.None:
            default:
                break;
        }
        var neighborPos = pos + offset;
        if (!IsValidPositionInChunk(neighborPos)) {
            var blockId = MapManager.instance.GetBlockIdByPosition(_chunkPosition * Config.ChunkSize + neighborPos);
            return blockId == 0;
        }
        var neighborBlockData = GetBlockData(neighborPos);
        if (neighborBlockData == null) {
            return true;
        }

        return neighborBlockData.Value.BlockId == 0;
    }
    
    private void UpdateMesh() {
        var meshTool = new SurfaceTool();
        var waterMeshTool = new SurfaceTool();
        meshTool.Begin(Mesh.PrimitiveType.Triangles);
        meshTool.SetMaterial(MaterialManager.instance.GetMaterial());
        waterMeshTool.Begin(Mesh.PrimitiveType.Triangles);
        waterMeshTool.SetMaterial(MaterialManager.instance.GetWaterMaterial());
        var baseIndex = 0;
        var waterBaseIndex = 0;
        for (var x = 0; x < Config.ChunkSize; x++) {
            for (var y = 0; y < Config.ChunkSize; y++) {
                for (var z = 0; z < Config.ChunkSize; z++) {
                    var blockData = _blockData[x][y][z];
                    if (blockData.BlockId == 0) continue;
                    if (BlockManager.instance.GetBlock(blockData.BlockId).transparent) {
                        // water or gas
                        var flags = 0;
                        var point = new Vector3I(x, y, z);
                        if (ShouldBeRender(point, Direction.North)) {
                            flags |= 1 << (int)Direction.North;
                        }
                        if (ShouldBeRender(point, Direction.South)) {
                            flags |= 1 << (int)Direction.South;
                        }
                        if (ShouldBeRender(point, Direction.East)) {
                            flags |= 1 << (int)Direction.East;
                        }
                        if (ShouldBeRender(point, Direction.West)) {
                            flags |= 1 << (int)Direction.West;
                        }
                        if (ShouldBeRender(point, Direction.Up)) {
                            flags |= 1 << (int)Direction.Up;
                        }
                        if (ShouldBeRender(point, Direction.Down)) {
                            flags |= 1 << (int)Direction.Down;
                        }
                        AddCubeMesh(waterMeshTool, blockData.BlockId, flags, ref waterBaseIndex, point);
                    } else {
                        var flags = 0;
                        var point = new Vector3I(x, y, z);
                        if (ShouldBeVisible(point, Direction.North)) {
                            flags |= 1 << (int)Direction.North;
                        }
                        if (ShouldBeVisible(point, Direction.South)) {
                            flags |= 1 << (int)Direction.South;
                        }
                        if (ShouldBeVisible(point, Direction.East)) {
                            flags |= 1 << (int)Direction.East;
                        }
                        if (ShouldBeVisible(point, Direction.West)) {
                            flags |= 1 << (int)Direction.West;
                        }
                        if (ShouldBeVisible(point, Direction.Up)) {
                            flags |= 1 << (int)Direction.Up;
                        }
                        if (ShouldBeVisible(point, Direction.Down)) {
                            flags |= 1 << (int)Direction.Down;
                        }
                        AddCubeMesh(meshTool, blockData.BlockId, flags, ref baseIndex, point);    
                    }
                }
            }
        }
        _colliderMesh = meshTool.Commit();
        var waterMesh = waterMeshTool.Commit(meshTool.Commit());
        _readyMesh = waterMesh;
    }

    private static Vector2[] GetUV(ulong blockId, Direction direction) {
        return MaterialManager.instance.GetUVs(blockId, direction);
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

    private void UpdateCollider() {
        if (_colliderMesh == null || _colliderMesh.GetSurfaceCount() == 0) return;
        var shape = _colliderMesh.CreateTrimeshShape();
        if (shape == null) {
            GD.Print("创建网格碰撞器失败");
            return;
        }

        var chunkCollision = GetNodeOrNull<CollisionShape3D>("ChunkStaticBody/Shape");
        if (chunkCollision != null) {
            chunkCollision.SetShape(shape);
        } else {
            var staticBody = new StaticBody3D();
            staticBody.Name = "ChunkStaticBody";
            var shapeBody = new CollisionShape3D();
            staticBody.AddChild(shapeBody);
            shapeBody.Name = "Shape";
            shapeBody.Shape = shape;
            AddChild(staticBody);
        }
    }
}