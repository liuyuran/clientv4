using Godot;
using System.Collections.Generic;
using System.Linq;
using game.scripts.manager;
using game.scripts.utils;

namespace game.scripts.renderer;

public partial class WorldRender: Node3D {
    private const int ChunkSize = 16;
    private const int ChunkRenderDistance = 1;
    private readonly Dictionary<Vector3I, ChunkRenderItem> _loadedChunks = new();
    
    private static Vector3I GetChunkCoordinates(Vector3 position) {
        return new Vector3I((int)Mathf.Floor(position.X / ChunkSize),
                (int)Mathf.Floor(position.Y / ChunkSize),
                (int)Mathf.Floor(position.Z / ChunkSize));
    }
    
    private IEnumerable<Vector3I> GetLoadedChunkCoordinates() {
        return _loadedChunks.Keys;
    }
    
    private static IEnumerable<Vector3I> GetRequiredChunkCoordinates(Vector3 playerPosition) {
        var centerChunk = GetChunkCoordinates(playerPosition);
        for (var x = centerChunk.X - ChunkRenderDistance; x <= centerChunk.X + ChunkRenderDistance; x++)
            for (var y = centerChunk.Y - ChunkRenderDistance; y <= centerChunk.Y + ChunkRenderDistance; y++)
                for (var z = centerChunk.Z - ChunkRenderDistance; z <= centerChunk.Z + ChunkRenderDistance; z++)
                    yield return new Vector3I(x, y, z);
    }

    public override void _Ready() {
        ResourcePackManager.instance.ScanResourcePacks();
        MaterialManager.instance.GenerateMaterials();
    }

    public override void _Process(double delta) {
        var player = GetParent().GetNode<Node3D>("Player");
        if (player == null) return;
        
        var requiredChunks = GetRequiredChunkCoordinates(player.Position).ToHashSet();
        var loadedChunks = GetLoadedChunkCoordinates().ToHashSet();
        
        // Load new chunks
        foreach (var chunkCoord in requiredChunks.Except(loadedChunks)) {
            var chunk = new ChunkRenderItem();
            var data =  new BlockData[ChunkSize][][];
            if (chunkCoord.Y > 0) {
                for (var x = 0; x < ChunkSize; x++) {
                    data[x] = new BlockData[ChunkSize][];
                    for (var y = 0; y < ChunkSize; y++) {
                        data[x][y] = new BlockData[ChunkSize];
                        for (var z = 0; z < ChunkSize; z++) {
                            data[x][y][z] = new BlockData {
                                BlockId = 0,
                                Direction = Direction.None
                            };
                        }
                    }
                }
            } else if (chunkCoord.Y == 0) {
                for (var x = 0; x < ChunkSize; x++) {
                    data[x] = new BlockData[ChunkSize][];
                    for (var y = 0; y < ChunkSize; y++) {
                        data[x][y] = new BlockData[ChunkSize];
                        for (var z = 0; z < ChunkSize; z++) {
                            data[x][y][z] = new BlockData {
                                BlockId = (ulong)(y <= 0 ? 1 : 0),
                                Direction = Direction.None
                            };
                        }
                    }
                }
            } else {
                for (var x = 0; x < ChunkSize; x++) {
                    data[x] = new BlockData[ChunkSize][];
                    for (var y = 0; y < ChunkSize; y++) {
                        data[x][y] = new BlockData[ChunkSize];
                        for (var z = 0; z < ChunkSize; z++) {
                            data[x][y][z] = new BlockData {
                                BlockId = 1,
                                Direction = Direction.None
                            };
                        }
                    }
                }
            }
            chunk.InitData(chunkCoord, data);
            AddChild(chunk);
            chunk.Position = new Vector3(
                chunkCoord.X * ChunkSize,
                chunkCoord.Y * ChunkSize,
                chunkCoord.Z * ChunkSize
            );
            _loadedChunks[chunkCoord] = chunk;
        }
        
        // Unload distant chunks
        foreach (var chunkCoord in loadedChunks.Except(requiredChunks)) {
            if (!_loadedChunks.TryGetValue(chunkCoord, out var chunk)) continue;
            chunk.QueueFree();
            _loadedChunks.Remove(chunkCoord);
        }
    }
}