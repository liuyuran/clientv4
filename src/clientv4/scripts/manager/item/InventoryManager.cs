using System.Collections.Generic;
using System.Linq;
using game.scripts.manager.item.composition;

namespace game.scripts.manager.item;

/// <summary>
/// manage equipment and inventory data for players.
/// </summary>
public class InventoryManager {
    public static InventoryManager instance { get; private set; } = new();
    private readonly Dictionary<ulong, Dictionary<ulong, Item>> _equipment = new();
    private const uint BaseToolSlot = 2;
    private const uint BaseInventorySlot = 2;

    /// <summary>
    /// Get total slots of tool from all equipment of player.
    /// </summary>
    public ulong GetToolSlotCount(ulong playerId) {
        if (!_equipment.TryGetValue(playerId, out var equipment)) {
            return BaseToolSlot;
        }

        return equipment.Select(pair => pair.Value).Where(item => item.IsEquipable())
            .Aggregate(BaseToolSlot, (current, item) => current + item.GetToolSlot());
    }

    /// <summary>
    /// Get total slots of inventory from all equipment of player.
    /// </summary>
    public ulong GetInventorySlotCount(ulong playerId) {
        if (!_equipment.TryGetValue(playerId, out var equipment)) {
            return BaseInventorySlot;
        }

        return equipment.Select(pair => pair.Value).Where(item => item.IsEquipable())
            .Aggregate(BaseInventorySlot, (current, item) => current + item.GetInventorySlot());
    }

    /// <summary>
    /// Get all items in player's inventory.
    /// </summary>
    public Item[] GetInventoryItems(ulong playerId) {
        if (!_equipment.TryGetValue(playerId, out var equipment)) {
            return [];
        }

        return equipment.Select(pair => pair.Value).Where(item => item.IsEquipable())
            .SelectMany(item => item.GetAllInventoryItems()).ToArray();
    }

    /// <summary>
    /// Get all tool items from player's equipment.
    /// </summary>
    public Item[] GetToolItems(ulong playerId) {
        if (!_equipment.TryGetValue(playerId, out var equipment)) {
            return [];
        }

        return equipment.Select(pair => pair.Value).Where(item => item.IsEquipable())
            .SelectMany(item => item.GetAllToolItems()).ToArray();
    }

    /// <summary>
    /// Add an item to the player's inventory.
    /// </summary>
    public ulong AddItemToInventory(ulong playerId, ulong itemId, ulong amount) {
        if (!_equipment.TryGetValue(playerId, out var equipments)) {
            return amount;
        }

        var count = amount;
        foreach (var (_, equipment) in equipments) {
            if (!equipment.IsEquipable()) {
                continue;
            }

            var items = equipment.GetAllInventoryItems();
            for (var index = 0; index < items.Length; index++) {
                var item = items[index];
                if (item == null) {
                    items[index] = ItemManager.instance.GetItemPrototype(itemId);
                    if (items[index].maxStack <= amount) {
                        items[index].stackCount = items[index].maxStack;
                        count -= items[index].maxStack;
                    } else {
                        items[index].stackCount = amount;
                        count = 0;
                    }
                } else if (itemId == ItemManager.instance.GetItemId(item.name)) {
                    if (item.stackCount + amount <= item.maxStack) {
                        item.stackCount += amount;
                        return 0;
                    }

                    count -= item.maxStack - item.stackCount;
                    item.stackCount = item.maxStack;
                }
                if (count == 0) return 0;
            }
        }

        return count;
    }

    /// <summary>
    /// Remove an item from the player's inventory.
    /// </summary>
    public ulong RemoveItemFromInventory(ulong playerId, ulong itemId, ulong amount) {
        if (!_equipment.TryGetValue(playerId, out var equipments)) {
            return amount;
        }

        var count = amount;
        foreach (var (_, equipment) in equipments) {
            if (!equipment.IsEquipable()) {
                continue;
            }

            var items = equipment.GetAllInventoryItems();
            foreach (var item in items) {
                if (itemId == ItemManager.instance.GetItemId(item.name)) {
                    if (item.stackCount >= amount) {
                        count = equipment.RemoveItem(item, amount);
                        if (count == 0) return 0;
                    }
                }
            }
        }

        return count;
    }

    /// <summary>
    /// Equip an item to player.
    /// </summary>
    /// <param name="playerId">the player id from PlayerManager</param>
    /// <param name="itemId">the item id from ItemManager</param>
    public void EquipItem(ulong playerId, ulong itemId) {
        if (!_equipment.TryGetValue(playerId, out var equipment)) {
            equipment = new Dictionary<ulong, Item>();
            _equipment[playerId] = equipment;
        }

        if (!ItemManager.instance.GetItem(itemId).IsEquipable()) {
            throw new System.Exception($"Item {itemId} is not equipable");
        }

        var item = ItemManager.instance.GetItem(itemId);
        var slot = item.GetEquipmentSlot();

        if (!equipment.TryAdd(slot, item)) {
            throw new System.Exception($"Slot {slot} is already occupied");
        }
    }

    /// <summary>
    /// Unequip an item from player.
    /// </summary>
    /// <param name="playerId">the player id from PlayerManager</param>
    /// <param name="slot">global equipment slot index of InventoryManager</param>
    public void UnequipItem(ulong playerId, ulong slot) {
        if (!_equipment.TryGetValue(playerId, out var equipment)) {
            throw new System.Exception($"Player {playerId} has no equipment");
        }

        if (!equipment.Remove(slot)) {
            throw new System.Exception($"Slot {slot} is not occupied");
        }
    }

    /// <summary>
    /// Get all equipment of a player.
    /// </summary>
    public Item[] GetAllEquipment(ulong playerId) {
        if (!_equipment.TryGetValue(playerId, out var equipment)) {
            return [];
        }

        return equipment.Values.ToArray();
    }
}