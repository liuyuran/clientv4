using System.Linq;

namespace game.scripts.manager.item.composition;

public static class Equipment {
    private const string EquipmentConfigKey = "equipment";
    
    public static bool IsEquipable(this Item item) {
        return item.Config.ContainsKey(EquipmentConfigKey);
    }

    public static void SetEquipment(this Item item, EquipmentConfig equipment) {
        item.Config[EquipmentConfigKey] = equipment;
    }
    
    public static uint GetToolSlot(this Item item) {
        if (!item.Config.TryGetValue(EquipmentConfigKey, out var value)) {
            return 0;
        }
        
        if (value is EquipmentConfig config) {
            return config.ToolSlot;
        }

        return 0;
    }
    
    public static ulong GetEquipmentSlot(this Item item) {
        if (!item.Config.TryGetValue(EquipmentConfigKey, out var value)) {
            return 0;
        }
        
        if (value is EquipmentConfig config) {
            return config.EquipmentSlot;
        }
        
        return 0;
    }
    
    public static uint GetInventorySlot(this Item item) {
        if (!item.Config.TryGetValue(EquipmentConfigKey, out var value)) {
            return 0;
        }
        
        if (value is EquipmentConfig config) {
            return config.InventorySlot;
        }
        
        return 0;
    }
    
    public static Item[] GetAllToolItems(this Item item) {
        if (!item.Config.TryGetValue(EquipmentConfigKey, out var value)) {
            return [];
        }
        
        if (value is EquipmentConfig config) {
            return config.items.Take((int)config.ToolSlot).ToArray();
        }

        return [];
    }
    
    public static Item[] GetAllInventoryItems(this Item item) {
        if (!item.Config.TryGetValue(EquipmentConfigKey, out var value)) {
            return [];
        }
        
        if (value is EquipmentConfig config) {
            return config.items.Skip((int)config.ToolSlot).Take((int)config.InventorySlot).ToArray();
        }

        return [];
    }

    public static ulong AddItem(this Item item, Item itemToAdd) {
        if (!item.Config.TryGetValue(EquipmentConfigKey, out var value)) {
            return itemToAdd.stackCount;
        }

        var count = itemToAdd.stackCount;
        if (value is EquipmentConfig config) {
            for (uint i = 0; i < config.items.Length; i++) {
                if (config.items[i].name == itemToAdd.name) {
                    if (config.items[i].stackCount + count <= config.items[i].maxStack) {
                        config.items[i].stackCount += itemToAdd.stackCount;
                        return 0;
                    }

                    count -= config.items[i].maxStack - config.items[i].stackCount;
                    config.items[i].stackCount = config.items[i].maxStack;
                } else if (config.items[i] == null) {
                    itemToAdd.stackCount = count;
                    config.items[i] = itemToAdd;
                    return 0;
                }
            }
        }

        return count;
    }
    
    public static bool AddItem(this Item item, int index, Item itemToAdd) {
        if (!item.Config.TryGetValue(EquipmentConfigKey, out var value)) {
            return false;
        }
        
        if (value is EquipmentConfig config) {
            if (index < 0 || index >= config.items.Length) {
                return false;
            }

            if (config.items[index] == null) {
                config.items[index] = itemToAdd;
                return true;
            }
        }

        return false;
    }

    public static ulong RemoveItem(this Item item, Item itemToRemove, ulong amount = 1) {
        if (!item.Config.TryGetValue(EquipmentConfigKey, out var value)) {
            return amount;
        }
        
        var count = amount;
        if (value is EquipmentConfig config) {
            for (uint i = 0; i < config.items.Length; i++) {
                if (config.items[i].name == itemToRemove.name) {
                    if (config.items[i].stackCount >= count) {
                        config.items[i].stackCount -= count;
                        if (config.items[i].stackCount == 0) {
                            config.items[i] = null;
                        }
                        return 0;
                    }

                    count -= config.items[i].stackCount; 
                    config.items[i] = null;
                }
            }
        }

        return count;
    }

    public static bool RemoveItem(this Item item, int index) {
        if (!item.Config.TryGetValue(EquipmentConfigKey, out var value)) {
            return false;
        }

        if (value is EquipmentConfig config) {
            if (index < 0 || index >= config.items.Length) {
                return false;
            }

            if (config.items[index] != null) {
                config.items[index] = null;
                return true;
            }
        }

        return false;
    }

    public static void MoveItem(this Item item, uint from, uint to) {
        if (!item.Config.TryGetValue(EquipmentConfigKey, out var value)) {
            return;
        }

        if (value is not EquipmentConfig config) return;
        if (from >= config.items.Length || to >= config.items.Length) {
            return;
        }

        (config.items[to], config.items[from]) = (config.items[from], config.items[to]);
    }

    public struct EquipmentConfig(ulong equipmentSlot, uint toolSlot, uint inventorySlot) {
        public readonly ulong EquipmentSlot = equipmentSlot;
        public readonly uint ToolSlot = toolSlot;
        public readonly uint InventorySlot = inventorySlot;
        public Item[] items { get; set; } = new Item[toolSlot + inventorySlot];
    }
}