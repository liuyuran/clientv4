using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using game.scripts.manager;
using game.scripts.manager.blocks;
using game.scripts.manager.map;
using game.scripts.renderer;
using game.scripts.utils;
using Godot;
using ModLoader.config;
using ModLoader.map.util;
using ModLoader.util;
using Vector3I = Godot.Vector3I;

namespace game.scripts.server.ECSBridge.render;

/// <summary>
/// used to render chunk entity that created by SChunkLoadSystem
/// </summary>
/// <see cref="SChunkLoadSystem"/>
public class SChunkRenderSystem(EntityStore world) : QuerySystem<CNodeLink, CGridIndex> {
    private readonly ConcurrentDictionary<Vector4I, CNodeLink> _chunkCache = new(); // cache chunk nodes via its location
    private readonly ConcurrentBag<Vector4I> _processing = []; // processing chunk render thread
    private readonly ComponentIndex<CGridIndex, Vector4I> _index = world.ComponentIndex<CGridIndex, Vector4I>(); 

    protected override void OnUpdate() {
        if (!_processing.IsEmpty) return;
        var commandBuffer = world.GetCommandBuffer().Synced;
        var chunkNeedUpdate = new ConcurrentBag<Vector4I>();
        Query.ForEachEntity((ref CNodeLink link, ref CGridIndex grid, Entity entity) => {
            if (!link.Dirty) return;
            chunkNeedUpdate.Add(grid.GetIndexedValue());
            link.Dirty = false;
        });
        var chunkNeedUpdateKeys = chunkNeedUpdate.ToArray();
        var groupTask = WorkerThreadPool.AddGroupTask(Callable.From<int>(index => {
            _processing.Add(chunkNeedUpdateKeys[index]);
            var unit = _index[chunkNeedUpdateKeys[index]];
            UpdateGridNode(chunkNeedUpdateKeys[index], unit, ref commandBuffer);
            _processing.TryTake(out _);
        }), chunkNeedUpdateKeys.Length);
        WorkerThreadPool.WaitForGroupTaskCompletion(groupTask);
        commandBuffer.Playback();
    }
    
    private void UpdateGridNode(Vector4I position, Entities entities, ref CommandBufferSynced commandBufferSynced) {
        var worldId = position.X;
        var chunkLocation = new Vector3I(position.Y, position.Z, position.W);
        var chunkData = MapManager.instance.GetBlockData(worldId, chunkLocation);
        var colliderMesh = UpdateChunkCube(position, chunkData);
        UpdateChunkCollider(position, colliderMesh);
        foreach (var entity in entities) {
            commandBufferSynced.AddComponent(entity.Id, _chunkCache[position]);
        }
    }
    
    /// <summary>
    /// update the mesh of the chunk, only for clients
    /// </summary>
    private ArrayMesh UpdateChunkCube(Vector4I position, BlockData[][][] chunkData) {
        if (!_chunkCache.TryGetValue(position, out var link)) {
            link = CreateNode(position);
            _chunkCache.TryAdd(position, link);
        }
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
                    var blockData = chunkData[x][y][z];
                    if (blockData.BlockId == 0) continue;
                    if (BlockManager.instance.GetBlock(blockData.BlockId).transparent) {
                        // water or gas
                        var flags = 0;
                        var point = new Vector3I(x, y, z);
                        if (ShouldBeRender(chunkData, point, Direction.North)) {
                            flags |= 1 << (int)Direction.North;
                        }
                        if (ShouldBeRender(chunkData, point, Direction.South)) {
                            flags |= 1 << (int)Direction.South;
                        }
                        if (ShouldBeRender(chunkData, point, Direction.East)) {
                            flags |= 1 << (int)Direction.East;
                        }
                        if (ShouldBeRender(chunkData, point, Direction.West)) {
                            flags |= 1 << (int)Direction.West;
                        }
                        if (ShouldBeRender(chunkData, point, Direction.Up)) {
                            flags |= 1 << (int)Direction.Up;
                        }
                        if (ShouldBeRender(chunkData, point, Direction.Down)) {
                            flags |= 1 << (int)Direction.Down;
                        }
                        AddCubeMesh(waterMeshTool, blockData.BlockId, flags, ref waterBaseIndex, point);
                    } else {
                        var flags = 0;
                        var point = new Vector3I(x, y, z);
                        if (ShouldBeVisible(chunkData, point, Direction.North)) {
                            flags |= 1 << (int)Direction.North;
                        }
                        if (ShouldBeVisible(chunkData, point, Direction.South)) {
                            flags |= 1 << (int)Direction.South;
                        }
                        if (ShouldBeVisible(chunkData, point, Direction.East)) {
                            flags |= 1 << (int)Direction.East;
                        }
                        if (ShouldBeVisible(chunkData, point, Direction.West)) {
                            flags |= 1 << (int)Direction.West;
                        }
                        if (ShouldBeVisible(chunkData, point, Direction.Up)) {
                            flags |= 1 << (int)Direction.Up;
                        }
                        if (ShouldBeVisible(chunkData, point, Direction.Down)) {
                            flags |= 1 << (int)Direction.Down;
                        }
                        AddCubeMesh(meshTool, blockData.BlockId, flags, ref baseIndex, point);    
                    }
                }
            }
        }
        var colliderMesh = meshTool.Commit();
        if (link.ClientNode != null) {
            var waterMesh = waterMeshTool.Commit(meshTool.Commit());
            link.ClientNode.Mesh = waterMesh;
        }
        return colliderMesh;
    }

    /// <summary>
    /// update the collider of the chunk, for server and client
    /// </summary>
    private void UpdateChunkCollider(Vector4I position, ArrayMesh colliderMesh) {
        if (!_chunkCache.TryGetValue(position, out var link)) {
            link = CreateNode(position);
            _chunkCache.TryAdd(position, link);
        }
        
        if (colliderMesh == null || colliderMesh.GetSurfaceCount() == 0) return;
        var shape = colliderMesh.CreateTrimeshShape();
        if (shape == null) {
            GD.Print("创建网格碰撞器失败");
            return;
        }

        var chunkCollision = link.ServerNode.FindNodeByName<CollisionShape3D>("shape");
        if (chunkCollision != null) {
            chunkCollision.SetShape(shape);
        } else {
            var staticBody = link.ServerNode;
            var shapeBody = new CollisionShape3D();
            staticBody.AddChild(shapeBody);
            shapeBody.Name = "Shape";
            shapeBody.Shape = shape;
        }
    }
    
    /// <summary>
    /// create the client node and server node for a specific chunk
    /// </summary>
    private CNodeLink CreateNode(Vector4I position) {
        var shouldNotUpdateRender = PlatformUtil.isDedicatedServer;
        var worldId = position.X;
        var chunkLocation = new Vector3I(position.Y, position.Z, position.W);
        var worldContainer = GameNodeReference.WorldContainer.GetSubViewport(worldId);
        var chunkContainer = worldContainer.FindNodeByName<WorldRender>("ChunkContainer");
        var chunkNode = new MeshInstance3D();
        chunkContainer.CallThreadSafe(Node.MethodName.AddChild, chunkNode);
        chunkNode.Name = $"Chunk_Render_{chunkLocation}";
        var staticBody = new StaticBody3D();
        staticBody.Name = "ChunkStaticBody";
        var link = new CNodeLink {
            ClientNode = shouldNotUpdateRender ? null : chunkNode,
            ServerNode = staticBody,
            Dirty = false
        };
        return link;
    }
    
    private static bool IsValidPositionInChunk(Vector3I pos) {
        return pos.X >= 0 && pos.X < Config.ChunkSize &&
               pos.Y >= 0 && pos.Y < Config.ChunkSize &&
               pos.Z >= 0 && pos.Z < Config.ChunkSize;
    }
    
    private BlockData? GetBlockData(BlockData[][][] chunkData, Vector3I pos) {
        if (!IsValidPositionInChunk(pos)) {
            return null;
        }
        return chunkData[pos.X][pos.Y][pos.Z];
    }
    
    private bool ShouldBeVisible(BlockData[][][] chunkData, Vector3I pos, Direction direction) {
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
            return true;
        }
        var neighborBlockData = GetBlockData(chunkData, neighborPos);
        if (neighborBlockData == null) {
            return true;
        }

        if (neighborBlockData.Value.BlockId == 0) return true;
        var block = BlockManager.instance.GetBlock(neighborBlockData.Value.BlockId);
        return block.transparent;
    }
    
    private bool ShouldBeRender(BlockData[][][] chunkData, Vector3I pos, Direction direction) {
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
            return true;
        }
        var neighborBlockData = GetBlockData(chunkData, neighborPos);
        if (neighborBlockData == null) {
            return true;
        }

        return neighborBlockData.Value.BlockId == 0;
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
}