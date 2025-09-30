using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using game.scripts.manager;
using game.scripts.manager.blocks;
using game.scripts.manager.map;
using game.scripts.utils;
using Godot;
using Godot.Collections;
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
    private readonly ConcurrentBag<(CollisionShape3D, ConcavePolygonShape3D, StaticBody3D)> _postProcess = [];

    protected override void OnUpdate() {
        if (!_processing.IsEmpty) return;
        var commandBuffer = world.GetCommandBuffer().Synced;
        var chunkNeedUpdate = new HashSet<Vector4I>();
        Query.ForEachEntity((ref CNodeLink link, ref CGridIndex grid, Entity entity) => {
            if (!link.Dirty) return;
            chunkNeedUpdate.Add(grid.GetIndexedValue());
            link.Dirty = false;
        });
        var chunkNeedUpdateKeys = chunkNeedUpdate.ToArray();
        foreach (var position in chunkNeedUpdateKeys) {
            if (_processing.Contains(position)) continue;
            if (!_chunkCache.TryGetValue(position, out var link)) {
                link = CreateNode(position);
                _chunkCache.TryAdd(position, link);
            }
        }
        
        var groupTask = WorkerThreadPool.AddGroupTask(Callable.From<int>(index => {
            _processing.Add(chunkNeedUpdateKeys[index]);
            var unit = _index[chunkNeedUpdateKeys[index]];
            UpdateGridNode(chunkNeedUpdateKeys[index], unit, ref commandBuffer);
            _processing.TryTake(out _);
        }), chunkNeedUpdateKeys.Length);
        WorkerThreadPool.WaitForGroupTaskCompletion(groupTask);
        while (!_postProcess.IsEmpty) {
            if (!_postProcess.TryTake(out var result)) continue;
            SetShapeAndAppendToNode(result.Item1, result.Item2, result.Item3);
        }
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
    private Array UpdateChunkCube(Vector4I position, BlockData[][][] chunkData) {
        if (!_chunkCache.TryGetValue(position, out var link)) {
            return null;
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

        var colliderMesh = meshTool.CommitToArrays();
        if (link.ClientNode != null) {
            var waterMesh = waterMeshTool.Commit(meshTool.Commit());
            link.ClientNode.CallDeferred(MeshInstance3D.MethodName.SetMesh, waterMesh);
        }

        return colliderMesh;
    }

    /// <summary>
    /// update the collider of the chunk, for server and client
    /// </summary>
    private void UpdateChunkCollider(Vector4I position, Array colliderMesh) {
        if (!_chunkCache.TryGetValue(position, out var link)) {
            return;
        }

        if (colliderMesh == null || colliderMesh.Count == 0) return;
        var vertices = (Vector3[])colliderMesh[(int)Mesh.ArrayType.Vertex];
        var indices = (int[])colliderMesh[(int)Mesh.ArrayType.Index];
        var faces = new List<Vector3>();
        if (indices.Length == 0) {
            // 无索引：假设顶点按三角顺序排列
            for (var i = 0; i < vertices.Length - 2; i += 3) {
                faces.Add(vertices[i]);
                faces.Add(vertices[i + 1]);
                faces.Add(vertices[i + 2]);
            }
        } else {
            // 有索引：每 3 个索引组成一个三角面
            for (var i = 0; i < indices.Length - 2; i += 3) {
                if (indices[i] < vertices.Length && indices[i + 1] < vertices.Length && indices[i + 2] < vertices.Length) {
                    faces.Add(vertices[indices[i]]);
                    faces.Add(vertices[indices[i + 1]]);
                    faces.Add(vertices[indices[i + 2]]);
                }
            }
        }

        var shape = new ConcavePolygonShape3D();
        shape.SetFaces(faces.ToArray());

        var chunkCollision = link.ServerNode.FindNodeByName<CollisionShape3D>("shape");
        _postProcess.Add((chunkCollision, shape, link.ServerNode));
    }

    private void SetShapeAndAppendToNode(CollisionShape3D chunkCollision, ConcavePolygonShape3D shape, StaticBody3D staticBody) {
        if (chunkCollision != null) {
            chunkCollision.SetShape(shape);
        } else {
            var shapeBody = new CollisionShape3D();
            shapeBody.Name = "Shape";
            shapeBody.SetShape(shape);
            staticBody.AddChild(shapeBody);
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
        var chunkNode = new MeshInstance3D();
        if (!shouldNotUpdateRender) {
            worldContainer.CallDeferred(Node.MethodName.AddChild, chunkNode);
            chunkNode.CallDeferred(Node3D.MethodName.SetGlobalPosition, new Vector3(chunkLocation.X * Config.ChunkSize, chunkLocation.Y * Config.ChunkSize, chunkLocation.Z * Config.ChunkSize));
        }
        chunkNode.Name = $"Chunk_Render_{chunkLocation}";
        var staticBody = new StaticBody3D();
        staticBody.Name = $"ChunkStaticBody_{chunkLocation}";
        worldContainer.CallDeferred(Node.MethodName.AddChild, staticBody);
        staticBody.CallDeferred(Node3D.MethodName.SetGlobalPosition, new Vector3(chunkLocation.X * Config.ChunkSize, chunkLocation.Y * Config.ChunkSize, chunkLocation.Z * Config.ChunkSize));
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