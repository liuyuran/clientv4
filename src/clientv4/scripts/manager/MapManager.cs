using System.Collections.Generic;
using game.scripts.config;
using game.scripts.manager.map;
using game.scripts.renderer;
using game.scripts.utils;
using Godot;

namespace game.scripts.manager;

public class MapManager {
    public delegate void BlockChangedCallback(ulong worldId, Vector3 position, ulong blockId, Direction direction);
    public static MapManager instance { get; private set; } = new();
    private readonly TerrainGenerator _generator;
    private readonly Dictionary<ulong, Dictionary<Vector3I, BlockData[][][]>> _chunks = new();
    public event BlockChangedCallback OnBlockChanged;
    
    private MapManager() {
        // 初始化地图管理器
        _generator = new TerrainGenerator(123456);
    }

    public void SetBlock(ulong worldId, Vector3 position, ulong blockId, Direction direction) {
        if (!_chunks.TryGetValue(worldId, out var chunkData)) {
            _chunks.Add(worldId, new Dictionary<Vector3I, BlockData[][][]>());
        }
        chunkData = _chunks[worldId];
        var chunkPosition = position.ToChunkPosition();
        var localPosition = position.ToLocalPosition();
        if (localPosition.X < 0) localPosition.X += Config.ChunkSize;
        if (localPosition.Y < 0) localPosition.Y += Config.ChunkSize;
        if (localPosition.Z < 0) localPosition.Z += Config.ChunkSize;
        if (!chunkData.TryGetValue(chunkPosition, out var blockData)) {
            blockData = GetBlockData(worldId, chunkPosition);
        }
        // 设置块数据
        blockData[localPosition.X][localPosition.Y][localPosition.Z] = new BlockData {
            BlockId = blockId,
            Direction = direction
        };
        chunkData[chunkPosition] = blockData;
        _chunks[worldId] = chunkData;
        // 触发块改变事件
        OnBlockChanged?.Invoke(worldId, position, blockId, direction);
    }

    public BlockData[][][] GetBlockData(ulong worldId, Vector3I position, bool createIfNotExists = true) {
        if (!_chunks.TryGetValue(worldId, out var chunkData)) {
            _chunks.Add(worldId, new Dictionary<Vector3I, BlockData[][][]>());
        }
        chunkData = _chunks[worldId];
        if (chunkData.TryGetValue(position, out var blockData)) return blockData;
        // 如果不存在对应的块数据且不允许创建，则返回 null
        if (!createIfNotExists) {
            return null;
        }
        var data = _generator.GenerateTerrain(worldId, position);
        chunkData.Add(position, data);
        blockData = data;
        return blockData;
    }

    public ulong GetBlockIdByPosition(Vector3 staticBodyGlobalPosition) {
        var chunkPosition = new Vector3I(
            (int)Mathf.Floor(staticBodyGlobalPosition.X / Config.ChunkSize),
            (int)Mathf.Floor(staticBodyGlobalPosition.Y / Config.ChunkSize),
            (int)Mathf.Floor(staticBodyGlobalPosition.Z / Config.ChunkSize)
        );
        var localPosition = new Vector3I(
            (int)(staticBodyGlobalPosition.X % Config.ChunkSize),
            (int)(staticBodyGlobalPosition.Y % Config.ChunkSize),
            (int)(staticBodyGlobalPosition.Z % Config.ChunkSize)
        );
        if (localPosition.X < 0) localPosition.X += Config.ChunkSize;
        if (localPosition.Y < 0) localPosition.Y += Config.ChunkSize;
        if (localPosition.Z < 0) localPosition.Z += Config.ChunkSize;
        var blockData = GetBlockData(0, chunkPosition);
        return blockData[localPosition.X][localPosition.Y][localPosition.Z].BlockId;
    }
    
    public void OverwriteBlockData(ulong worldId, Vector3I chunkPosition, BlockData[][][] blockData) {
        if (!_chunks.TryGetValue(worldId, out var chunkData)) {
            _chunks.Add(worldId, new Dictionary<Vector3I, BlockData[][][]>());
        }
        chunkData = _chunks[worldId];
        chunkData[chunkPosition] = blockData;
        _chunks[worldId] = chunkData;
    }
}