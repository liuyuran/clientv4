using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using game.scripts.manager.blocks;

namespace game.scripts.manager;

public class BlockManager {
    public static BlockManager instance { get; private set; } = new();
    
    private readonly ConcurrentDictionary<string, ulong> _blockIds = new();
    private readonly ConcurrentDictionary<ulong, Block> _blocks = new();
    private long _currentId;

    private BlockManager() {
        Register<Dirt>();
    }

    private void Register<T>() where T : Block, new() {
        var block = new T();
        var id = (ulong)Interlocked.Increment(ref _currentId);
        _blocks.TryAdd(id, block);
        _blockIds.TryAdd(block.Name, id);
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