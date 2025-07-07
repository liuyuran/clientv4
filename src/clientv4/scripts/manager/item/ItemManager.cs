using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using game.scripts.manager.item.composition;
using game.scripts.manager.reset;
using game.scripts.renderer;
using Godot;
using Microsoft.Extensions.Logging;
using ModLoader.logger;

namespace game.scripts.manager.item;

public class ItemManager: IReset, IDisposable {
    private readonly ILogger _logger = LogManager.GetLogger<ItemManager>();
    public static ItemManager instance { get; private set; } = new();
    
    private readonly ConcurrentDictionary<string, ulong> _itemIds = new();
    private readonly ConcurrentDictionary<ulong, Item> _items = new();
    private long _currentId;

    private ItemManager() {
        Register<Dirt>();
    }

    private void Register<T>() where T : Item, new() {
        var item = new T();
        var id = (ulong)Interlocked.Increment(ref _currentId);
        _items.TryAdd(id, item);
        _itemIds.TryAdd(item.name, id);
        _logger.LogDebug("Item {ItemName} registered with ID {ItemId}", item.name, id);
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
}