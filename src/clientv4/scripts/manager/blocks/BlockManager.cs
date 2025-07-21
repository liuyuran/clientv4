using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using game.scripts.manager.archive;
using game.scripts.manager.reset;
using generated.archive;
using Google.FlatBuffers;
using Microsoft.Extensions.Logging;
using ModLoader.logger;

namespace game.scripts.manager.blocks;

public class BlockManager: IReset, IArchive, IDisposable {
    private readonly ILogger _logger = LogManager.GetLogger<BlockManager>();
    public static BlockManager instance { get; private set; } = new();
    private const string ArchiveFilename = "block-define.dat";
    
    private readonly ConcurrentDictionary<string, ulong> _blockIds = new();
    private readonly ConcurrentDictionary<Type, ulong> _blockCache = new();
    private readonly ConcurrentDictionary<ulong, Block> _blocks = new();
    private ulong _currentId;

    private BlockManager() {
        Register<Water>();
        Register<Dirt>();
        Register<Stone>();
    }

    public void Register<T>() where T : Block, new() {
        var block = new T();
        if (_blockIds.ContainsKey(block.name)) return; // avoid duplicate registration
        var id = Interlocked.Increment(ref _currentId);
        _blocks.TryAdd(id, block);
        _blockCache.TryAdd(typeof(T), id);
        _blockIds.TryAdd(block.name, id);
        _logger.LogDebug("block {BlockName} registered with ID {BlockId}", block.name, id);
    }
    
    private void Register(ulong id, Block block) {
        if (_blockIds.ContainsKey(block.name)) return; // avoid duplicate registration
        _blocks.TryAdd(id, block);
        _blockCache.TryAdd(block.GetType(), id);
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

    public void Reset() {
        instance = new BlockManager();
        Dispose();
    }
    
    public void Dispose() {
        _blockCache.Clear();
        _blockIds.Clear();
        _blocks.Clear();
        GC.SuppressFinalize(this);
    }

    public void Archive(Dictionary<string, byte[]> fileList) {
        var fbb = new FlatBufferBuilder(1024);
        var dataList = new List<Offset<BlockDefineItem>>();
        foreach (var blockId in _blocks.Keys) {
            var type = fbb.CreateString(_blocks[blockId].GetType().FullName);
            BlockDefineItem.StartBlockDefineItem(fbb);
            BlockDefineItem.AddBlockId(fbb, blockId);
            BlockDefineItem.AddType(fbb, type);
            var item = BlockDefineItem.EndBlockDefineItem(fbb);
            dataList.Add(item);
        }

        var dataListOffset = fbb.CreateVectorOfTables(dataList.ToArray());
        BlockDefine.StartBlockDefine(fbb);
        BlockDefine.AddIdBreakpoint(fbb, _currentId);
        BlockDefine.AddData(fbb, dataListOffset);
        var offset = BlockDefine.EndBlockDefine(fbb);
        fbb.Finish(offset.Value);
        fileList.Add(ArchiveFilename, fbb.SizedByteArray());
    }
    
    public void Recover(Func<string, byte[]> getDataFunc) {
        var data = getDataFunc(ArchiveFilename);
        if (data == null || data.Length == 0) {
            _logger.LogWarning("No block define data found in archive, skipping recovery.");
            return;
        }
        var blockDefine = BlockDefine.GetRootAsBlockDefine(new ByteBuffer(data));
        _currentId = blockDefine.IdBreakpoint;
        for (var i = 0; i < blockDefine.DataLength; i++) {
            var item = blockDefine.Data(i);
            if (item == null) {
                _logger.LogWarning("Block define item at index {Index} is null, skipping.", i);
                continue;
            }
            var id = item.Value.BlockId;
            var type = item.Value.Type;
            var blockType = Type.GetType(type);
            if (blockType == null) {
                _logger.LogWarning("Block type {Type} not found, skipping.", type);
                continue;
            }
            if (!typeof(Block).IsAssignableFrom(blockType)) {
                _logger.LogWarning("Type {Type} is not a Block, skipping.", type);
                continue;
            }
            var block = (Block)Activator.CreateInstance(blockType);
            if (block == null) {
                _logger.LogWarning("Failed to create instance of block type {Type}, skipping.", type);
                continue;
            }
            Register(id, block);
        }
    }
}