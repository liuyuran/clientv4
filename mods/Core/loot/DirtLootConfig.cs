using Core.item;
using ModLoader.loot;

namespace Core.loot;

public class DirtLootConfig: LootConfig {
    public override LootItem[] LootDropItem(float random, ulong playerId) {
        return [new LootItem {
            ItemName = new DirtItem().name,
            amount = 1,
            Data = new Dictionary<string, object>()
        }];
    }
}