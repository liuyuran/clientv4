using System.Collections.Generic;
using System.Threading.Tasks;
using game.scripts.config;
using game.scripts.manager.map.generator;
using game.scripts.renderer;
using game.scripts.utils;
using Godot;
using Microsoft.Extensions.Logging;
using ModLoader.logger;

namespace game.scripts.manager.map;

public class MapManager {
    private readonly ILogger _logger = LogManager.GetLogger<MapManager>();
    public delegate void BlockChangedCallback(ulong worldId, Vector3 position, ulong blockId, Direction direction);
    public static MapManager instance { get; private set; } = new();
    private readonly TerrainGenerator _generator;
    private readonly Dictionary<ulong, Dictionary<Vector3I, BlockData[][][]>> _chunks = new();
    public event BlockChangedCallback OnBlockChanged;
    
    private readonly Dictionary<(ulong, Vector3I), bool> _pendingGenerationTasks = new();
    private readonly object _lockObject = new();
    
    private MapManager() {
        // 初始化地图管理器
        _generator = new TerrainGenerator(123456);
    }
    
    public void RegisterGenerator<T>(ulong worldId) where T : IWorldGenerator {
        _generator.RegistryGenerator<T>(worldId);
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

    public BlockData[][][] GetBlockData(ulong worldId, Vector3I position, bool createIfNotExists = true, bool sync = true) {
        if (!_chunks.TryGetValue(worldId, out var chunkData)) {
            _chunks.Add(worldId, new Dictionary<Vector3I, BlockData[][][]>());
        }
        chunkData = _chunks[worldId];
        if (chunkData.TryGetValue(position, out var blockData)) return blockData;
        // 如果不存在对应的块数据且不允许创建，则返回 null
        if (!createIfNotExists) {
            return null;
        }

        lock (_lockObject) {
            if (_pendingGenerationTasks.TryGetValue((worldId, position), out var isGenerating) && isGenerating) {
                // Generation already in progress, return null
                return null;
            }
        
            // Mark as generating
            _pendingGenerationTasks[(worldId, position)] = true;
        }
    
        if (sync) {
            // Start generation in thread pool
            Task.Run(() => {
                GenerateChunk(worldId, position);
            });
        } else {
            GenerateChunk(worldId, position);
            // If not syncing, we can directly generate the chunk
            // and return the data immediately
            if (_chunks.TryGetValue(worldId, out chunkData) && chunkData.TryGetValue(position, out blockData)) {
                return blockData;
            }
        }
    
        // Return null since generation is in progress
        return null;
    }
    
    private void GenerateChunk(ulong worldId, Vector3I position) {
        try {
            var startTime = PlatformUtil.GetTimestamp();
            var data = _generator.GenerateTerrain(worldId, position);
            _logger.LogDebug("Generate terrain for chunk {position} in world {worldId} took {time} ms",
                position, worldId, PlatformUtil.GetTimestamp() - startTime);

            lock (_lockObject) {
                // Add the generated data to the chunk dictionary
                _chunks[worldId][position] = data;
                // Remove from pending tasks
                _pendingGenerationTasks.Remove((worldId, position));
            }
        } catch (System.Exception ex) {
            _logger.LogError(ex, "Error generating terrain for chunk {position} in world {worldId}", position, worldId);
            lock (_lockObject) {
                _pendingGenerationTasks.Remove((worldId, position));
            }
        }
    }

    public ulong? GetBlockIdByPosition(Vector3 staticBodyGlobalPosition) {
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
        return blockData?[localPosition.X][localPosition.Y][localPosition.Z].BlockId;
    }
    
    public void OverwriteBlockData(ulong worldId, Vector3I chunkPosition, BlockData[][][] blockData) {
        if (!_chunks.TryGetValue(worldId, out var chunkData)) {
            _chunks.Add(worldId, new Dictionary<Vector3I, BlockData[][][]>());
        }
        chunkData = _chunks[worldId];
        chunkData[chunkPosition] = blockData;
        _chunks[worldId] = chunkData;
        _logger.LogDebug("Overwrote block data for chunk {chunkPosition} in world {worldId}", chunkPosition, worldId);
    }

    public long GetNearestLand(ulong worldId, Vector3 position) {
        var chunkPosition = position.ToChunkPosition();
        var localPosition = position.ToLocalPosition();
        if (localPosition.X < 0) localPosition.X += Config.ChunkSize;
        if (localPosition.Y < 0) localPosition.Y += Config.ChunkSize;
        if (localPosition.Z < 0) localPosition.Z += Config.ChunkSize;
        
        // 计算全局Y坐标
        long globalY = chunkPosition.Y * Config.ChunkSize + localPosition.Y;
        
        // 搜索范围（确保覆盖上下一个区块）
        var searchRange = Config.ChunkSize * 2;
        
        // 先检查当前位置
        if (CheckPositionForEmptySpace(worldId, chunkPosition, localPosition, globalY)) {
            return globalY;
        }
        
        // 交替向下和向上搜索
        for (var i = 1; i <= searchRange; i++) {
            // 向下搜索
            var downY = globalY - i;
            if (CheckPositionForEmptySpace(worldId, chunkPosition, localPosition, downY)) {
                return downY;
            }
            
            // 向上搜索
            var upY = globalY + i;
            if (CheckPositionForEmptySpace(worldId, chunkPosition, localPosition, upY)) {
                return upY;
            }
        }
        
        return -1;  // 没找到合适的空间
    }
    
    // 辅助方法：检查指定全局Y坐标处是否有两格连续的空间
    private bool CheckPositionForEmptySpace(ulong worldId, Vector3I originalChunkPos, Vector3I localPos, long globalY) {
        // 计算区块位置和本地位置
        var chunkPos = new Vector3I(
            originalChunkPos.X,
            (int)Mathf.Floor((float)globalY / Config.ChunkSize),
            originalChunkPos.Z
        );
        var localY = (int)(globalY % Config.ChunkSize);
        if (localY < 0) localY += Config.ChunkSize;
        
        // 获取区块数据
        var blockData = GetBlockData(worldId, chunkPos);
        if (blockData == null) return false;
        
        // 检查当前位置是否为空
        if (blockData[localPos.X][localY][localPos.Z].BlockId != 0) return false;
        
        // 检查上方一格是否为空
        if (localY + 1 < Config.ChunkSize) {
            // 上方格子在同一区块
            return blockData[localPos.X][localY + 1][localPos.Z].BlockId == 0;
        }

        // 上方格子在上方区块
        var upChunkPos = new Vector3I(chunkPos.X, chunkPos.Y + 1, chunkPos.Z);
        var upBlockData = GetBlockData(worldId, upChunkPos, false);
        return upBlockData != null && upBlockData[localPos.X][0][localPos.Z].BlockId == 0;
    }
}