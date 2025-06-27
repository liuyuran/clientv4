using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using game.scripts.manager.blocks;
using Godot;
using Microsoft.Extensions.Logging;
using ModLoader.logger;

namespace game.scripts.manager;

public class BlockManager {
    private readonly ILogger _logger = LogManager.GetLogger<BlockManager>();
    public static BlockManager instance { get; private set; } = new();
    
    private readonly ConcurrentDictionary<string, ulong> _blockIds = new();
    private readonly ConcurrentDictionary<Type, ulong> _blockCache = new();
    private readonly ConcurrentDictionary<ulong, Block> _blocks = new();
    private long _currentId;

    private BlockManager() {
        Register<Water>();
        Register<Dirt>();
        Register<Stone>();
    }

    private void Register<T>() where T : Block, new() {
        var block = new T();
        var id = (ulong)Interlocked.Increment(ref _currentId);
        _blocks.TryAdd(id, block);
        _blockCache.TryAdd(typeof(T), id);
        _blockIds.TryAdd(block.name, id);
        _logger.LogDebug("block {BlockName} registered with ID {BlockId}", block.name, id);
    }

    public ulong GetBlockId<T>() where T : Block {
        if (_blockCache.TryGetValue(typeof(T), out var id)) {
            return id;
        }

        return 0;
    }

    public ulong GetBlockId(string name) {
        if (!_blockIds.TryGetValue(name, out var id)) {
            throw new Exception($"Block {name} not found");
        }
        return id;
    }

    public IEnumerable<ulong> GetBlockIds() {
        return _blockIds.Values;
    }

    public Block GetBlock(ulong blockId) {
        if (!_blocks.TryGetValue(blockId, out var block)) {
            throw new Exception($"BlockId {blockId} not found");
        }
        return block;
    }
}