using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using game.scripts.manager.reset;
using game.scripts.renderer;
using generated.archive;
using Godot;
using Google.FlatBuffers;
using Microsoft.Extensions.Logging;
using ModLoader.archive;
using ModLoader.handler;
using ModLoader.item;
using ModLoader.item.composition;
using ModLoader.logger;

namespace game.scripts.manager.item;

public class ItemManager : IReset, IArchive, IDisposable, IItemManager {
    private readonly ILogger _logger = LogManager.GetLogger<ItemManager>();
    public static ItemManager instance { get; private set; } = new();
    private const string ArchiveFilename = "item-define.dat";

    private readonly ConcurrentDictionary<string, ulong> _itemIds = new();
    private readonly ConcurrentDictionary<ulong, Item> _items = new();
    private ulong _currentId;

    public void Register<T>() where T : Item, new() {
        var item = new T();
        if (_itemIds.ContainsKey(item.name)) return; // avoid duplicate registration
        var id = Interlocked.Increment(ref _currentId);
        _items.TryAdd(id, item);
        _itemIds.TryAdd(item.name, id);
        _logger.LogDebug("Item {ItemName} registered with ID {ItemId}", item.name, id);
    }
    
    private void Register(ulong id, Item item) {
        if (_itemIds.ContainsKey(item.name)) return; // avoid duplicate registration
        _items.TryAdd(id, item);
        _itemIds.TryAdd(item.name, id);
        _logger.LogDebug("item {ItemName} registered with ID {ItemId}", item.name, id);
    }

    public ulong GetItemId(string name) {
        if (!_itemIds.TryGetValue(name, out var id)) {
            throw new Exception($"Item {name} not found");
        }

        return id;
    }

    public IEnumerable<ulong> GetItemIds() {
        return _itemIds.Values;
    }

    public Item GetItem(ulong itemId) {
        if (!_items.TryGetValue(itemId, out var item)) {
            throw new Exception($"ItemId {itemId} not found");
        }

        return item;
    }

    public Item GetItemPrototype(ulong itemId) {
        if (!_items.TryGetValue(itemId, out var item)) {
            throw new Exception($"ItemId {itemId} not found");
        }

        return item.Clone();
    }

    public Node3D GetItemDropModel(ulong itemId) {
        if (!_items.TryGetValue(itemId, out var item)) {
            throw new Exception($"ItemId {itemId} not found");
        }

        Node3D node;
        if (item.IsBlock()) {
            var childNode = new DropItem3D();
            childNode.SetItemId(itemId);
            node = childNode;
        } else {
            var childNode = new DropItem2D();
            childNode.SetItemId(itemId);
            node = childNode;
        }

        return node;
    }

    public void Reset() {
        instance = new ItemManager();
        Dispose();
    }

    public void Dispose() {
        _items.Clear();
        _itemIds.Clear();
        GC.SuppressFinalize(this);
    }

    public void Archive(Dictionary<string, byte[]> fileList) {
        var fbb = new FlatBufferBuilder(1024);
        var dataList = new List<Offset<ItemDefineItem>>();
        foreach (var itemId in _items.Keys) {
            var item = _items[itemId];
            var type = fbb.CreateString(item.GetType().FullName);
            ItemDefineItem.StartItemDefineItem(fbb);
            ItemDefineItem.AddItemId(fbb, itemId);
            ItemDefineItem.AddType(fbb, type);
            var itemOffset = ItemDefineItem.EndItemDefineItem(fbb);
            dataList.Add(itemOffset);
        }

        var dataListOffset = fbb.CreateVectorOfTables(dataList.ToArray());
        ItemDefine.StartItemDefine(fbb);
        BlockDefine.AddIdBreakpoint(fbb, _currentId);
        BlockDefine.AddData(fbb, dataListOffset);
        fbb.Finish(ItemDefine.EndItemDefine(fbb).Value);
        fileList[ArchiveFilename] = fbb.SizedByteArray();
    }

    public void Recover(Func<string, byte[]> getDataFunc) {
        var data = getDataFunc(ArchiveFilename);
        if (data == null || data.Length == 0) {
            _logger.LogWarning("No item define data found in archive, skipping recovery.");
            return;
        }
        var itemDefine = ItemDefine.GetRootAsItemDefine(new ByteBuffer(data));
        _currentId = itemDefine.IdBreakpoint;
        for (var i = 0; i < itemDefine.DataLength; i++) {
            var item = itemDefine.Data(i);
            if (item == null) {
                _logger.LogWarning("Item define item at index {Index} is null, skipping.", i);
                continue;
            }
            var id = item.Value.ItemId;
            var type = item.Value.Type;
            var itemType = Type.GetType(type);
            if (itemType == null) {
                _logger.LogWarning("Item type {Type} not found, skipping.", type);
                continue;
            }
            if (!typeof(Item).IsAssignableFrom(itemType)) {
                _logger.LogWarning("Type {Type} is not a Item, skipping.", type);
                continue;
            }
            var itemInstance = (Item)Activator.CreateInstance(itemType);
            if (itemInstance == null) {
                _logger.LogWarning("Failed to create instance of item type {Type}, skipping.", type);
                continue;
            }
            Register(id, itemInstance);
        }
    }
}