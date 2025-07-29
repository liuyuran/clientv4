using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using game.scripts.manager.archive;
using game.scripts.manager.reset;
using game.scripts.utils;
using generated.archive;
using Godot;
using Google.FlatBuffers;
using Microsoft.Extensions.Logging;
using ModLoader.config;
using ModLoader.handler;
using ModLoader.logger;
using ModLoader.map.generator;
using ModLoader.map.util;
using ModLoader.util;
using Vector3I = Godot.Vector3I;

namespace game.scripts.manager.map;

public class MapManager : IReset, IArchive, IDisposable, IMapManager {
    private readonly ILogger _logger = LogManager.GetLogger<MapManager>();
    private const string MapFilename = "world_{0}/chunk_{1}_{2}_{3}.dat";

    public delegate void BlockChangedCallback(ulong worldId, Vector3 position, ulong blockId, Direction direction);

    public static MapManager instance { get; private set; } = new();
    public static long Seed;
    private readonly TerrainGenerator _generator;
    private readonly Dictionary<ulong, Dictionary<Vector3I, BlockData[][][]>> _chunks = new();
    private readonly HashSet<(ulong, Vector3I)> _needArchive = [];
    public event BlockChangedCallback OnBlockChanged;

    private readonly Dictionary<(ulong, Vector3I), bool> _pendingGenerationTasks = new();
    private readonly object _lockObject = new();

    private MapManager() {
        // 初始化地图管理器
        _generator = new TerrainGenerator(Seed);
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
        lock (_lockObject) {
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
            _needArchive.Add((worldId, chunkPosition));
        }
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
            _needArchive.Add((worldId, position));
        }

        if (sync) {
            // Start generation in thread pool
            Task.Run(() => { GenerateChunk(worldId, position); });
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
            if (!_chunks.ContainsKey(worldId)) {
                _chunks.Add(worldId, new Dictionary<Vector3I, BlockData[][][]>());
            }
            if (_chunks[worldId].ContainsKey(position)) {
                return;
            }
            var data = TryGetBlockDataFromArchive(worldId, position);
            if (data == null) {
                var startTime = PlatformUtil.GetTimestamp();
                data = _generator.GenerateTerrain(worldId, position);
                _logger.LogDebug("Generate terrain for chunk {position} in world {worldId} took {time} ms",
                    position, worldId, PlatformUtil.GetTimestamp() - startTime);
            }

            lock (_lockObject) {
                // Add the generated data to the chunk dictionary
                _chunks[worldId][position] = data;
                // Remove from pending tasks
                _pendingGenerationTasks.Remove((worldId, position));
                _needArchive.Add((worldId, position));
            }
        } catch (Exception ex) {
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

        return -1; // 没找到合适的空间
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

    public void Reset() {
        instance = new MapManager();
        Dispose();
    }

    public void Dispose() {
        _chunks.Clear();
        _pendingGenerationTasks.Clear();
        OnBlockChanged = null;
        GC.SuppressFinalize(this);
    }
    
    private BlockData[][][] TryGetBlockDataFromArchive(ulong worldId, Vector3I chunkPosition) {
        var filename = string.Format(MapFilename, worldId, chunkPosition.X, chunkPosition.Y, chunkPosition.Z);
        var data = ArchiveManager.instance.GetFileAsBytesFromCurrentArchive(filename);
        if (data == null) {
            return null;
        }

        var index = 0;
        var chunkPackage = ChunkPackage.GetRootAsChunkPackage(new ByteBuffer(data));
        if (Config.ChunkSize * Config.ChunkSize * Config.ChunkSize != chunkPackage.DataLength) {
            return null;
        }
        var blockData = new BlockData[Config.ChunkSize][][];
        for (var x = 0; x < Config.ChunkSize; x++) {
            blockData[x] ??= new BlockData[Config.ChunkSize][];
            for (var y = 0; y < Config.ChunkSize; y++) {
                blockData[x][y] = new BlockData[Config.ChunkSize];
                for (var z = 0; z < Config.ChunkSize; z++) {
                    var dataItem = chunkPackage.Data(index++);
                    if (dataItem == null) {
                        return null;
                    }
                    blockData[x][y][z] = new BlockData {
                        BlockId = dataItem.Value.BlockId,
                        Direction = (Direction)dataItem.Value.Direction
                    };
                }
            }            
        }
        return blockData;
    }

    public void Archive(Dictionary<string, byte[]> fileList) {
        lock (_lockObject) {
            foreach (var (worldId, chunkPosition) in _needArchive) {
                if (_chunks.TryGetValue(worldId, out var chunkData) && 
                    chunkData.TryGetValue(chunkPosition, out var blockData)) {
                    // 序列化块数据
                    var fbb = new FlatBufferBuilder(1024);
                    var dataList = new List<Offset<ChunkBlockData>>();
                    for (var x = 0; x < Config.ChunkSize; x++) {
                        for (var y = 0; y < Config.ChunkSize; y++) {
                            for (var z = 0; z < Config.ChunkSize; z++) {
                                var block = blockData[x][y][z];
                                if (block.BlockId == 0) continue;
                                var blockOffset = ChunkBlockData.CreateChunkBlockData(fbb, block.BlockId, (uint)block.Direction);
                                dataList.Add(blockOffset);
                            }
                        }
                    }
                    var dataListOffset = fbb.CreateVectorOfTables(dataList.ToArray());
                    ChunkPackage.StartChunkPackage(fbb);
                    ChunkPackage.AddData(fbb, dataListOffset);
                    var offset = ChunkPackage.EndChunkPackage(fbb);
                    fbb.Finish(offset.Value);
                    fileList[string.Format(MapFilename, worldId, chunkPosition.X, chunkPosition.Y, chunkPosition.Z)] = fbb.SizedByteArray();
                } else {
                    _logger.LogWarning("Chunk data for world {WorldId} at position {ChunkPosition} not found", worldId, chunkPosition);
                }
            }
            _needArchive.Clear();
        }
    }

    public void Recover(Func<string, byte[]> getDataFunc) {
        // not do anything here
    }
}