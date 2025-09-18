using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using game.scripts.manager.map;
using game.scripts.manager.player;
using game.scripts.utils;
using Godot;
using ModLoader.config;
using ModLoader.util;
using Vector3I = Godot.Vector3I;

namespace game.scripts.server.ECSBridge.render;

/// <summary>
/// used to create, disable or destroy chunk entity.
/// well, but the disable operation is not implemented yet.
/// </summary>
/// <see cref="SChunkRenderSystem"/>
public class SChunkLoadSystem : QuerySystem<CNodeLink, CGridIndex> {
    private readonly ConcurrentDictionary<Vector4I, int> _loadedChunks = [];
    private readonly ConcurrentQueue<Vector4I> _dirtyChunks = new();
    private readonly ComponentIndex<CGridIndex, Vector4I> _index;

    public SChunkLoadSystem(EntityStore world) {
        MapManager.instance.OnBlockChanged += OnInstanceOnOnBlockChanged;
        _index = world.ComponentIndex<CGridIndex, Vector4I>();
    }
    
    private IEnumerable<Vector3I> GetRequiredChunkCoordinates(Vector3 playerPosition) {
        var centerChunk = playerPosition.ToChunkPosition();
        for (var x = centerChunk.X - Config.ChunkRenderDistance; x <= centerChunk.X + Config.ChunkRenderDistance; x++)
        for (var y = centerChunk.Y - Config.ChunkRenderDistance; y <= centerChunk.Y + Config.ChunkRenderDistance; y++)
        for (var z = centerChunk.Z - Config.ChunkRenderDistance; z <= centerChunk.Z + Config.ChunkRenderDistance; z++)
            yield return new Vector3I(x, y, z);
    }

    private void OnInstanceOnOnBlockChanged(int worldId, Vector3 position, ulong blockId, Direction direction) {
        var chunkPosition = position.ToChunkPosition();
        _dirtyChunks.Enqueue(new Vector4I(worldId, chunkPosition.X, chunkPosition.Y, chunkPosition.Z));
    }
    
    protected override void OnUpdate() {
        while (_dirtyChunks.TryDequeue(out var dirtyItem)) {
            if (!_loadedChunks.ContainsKey(dirtyItem)) continue;
            var entities = _index[dirtyItem];
            foreach (var entity in entities) {
                entity.GetComponent<CNodeLink>().Dirty = true;
            }
        }

        // query local player
        var requiredChunks = new HashSet<Vector4I>();
        // if master client or dedicated server, load all player's chunk
        var players = PlayerManager.instance.GetAllPlayers();
        foreach (var playerInfo in players) {
            var position = playerInfo.position;
            var worldId = playerInfo.worldId;
            using var iter = GetRequiredChunkCoordinates(position).GetEnumerator();
            while (iter.MoveNext()) {
                var chunkCoord = iter.Current;
                requiredChunks.Add(new Vector4I(worldId, chunkCoord.X, chunkCoord.Y, chunkCoord.Z));
            }
        }

        // load can be load, if not data, wait next tick
        var createCount = 0;
        var commandBuffer = CommandBuffer;
        var loadedChunks = _loadedChunks.Keys.ToHashSet();
        foreach (var chunkCoord in requiredChunks.Except(loadedChunks)) {
            _loadedChunks.TryAdd(chunkCoord, 1);
            // don't create too many chunks in one frame
            createCount++;
            if (createCount > 1) break;
            // create block entity
            var chunkPos = new Vector3I(chunkCoord.Y, chunkCoord.Z, chunkCoord.W);
            var chunkData = MapManager.instance.GetBlockData(chunkCoord.X, chunkPos);
            for (var x = 0; x < Config.ChunkSize; x++) {
                for (var y = 0; y < Config.ChunkSize; y++) {
                    for (var z = 0; z < Config.ChunkSize; z++) {
                        var blockData = chunkData[x][y][z];
                        var entityId = commandBuffer.CreateEntity();
                        commandBuffer.AddComponent(entityId, new CGridIndex(chunkCoord.X, chunkPos));
                        commandBuffer.AddComponent(entityId, new CNodeLink {
                            ServerNode = null,
                            ClientNode = null,
                            Dirty = true
                        });
                        commandBuffer.AddComponent(entityId, new CBlockInfo {
                            BlockId = blockData.BlockId,
                            LocalPos = new Vector3I(x, y, z)
                        });
                    }
                }
            }
        }

        // unload chunks that are no longer required
        foreach (var chunkCoord in loadedChunks.Except(requiredChunks)) {
            _loadedChunks.Remove(chunkCoord, out _);
        }
    }
}