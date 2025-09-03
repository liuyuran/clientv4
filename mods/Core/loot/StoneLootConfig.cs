using Core.block;
using Core.item;
using ModLoader.loot;

namespace Core.loot;

public class StoneLootConfig: LootConfig {
    public override string blockName => new Stone().name;
    public override string[] allDropItem => [new DirtItem().name];

    public override LootItem[] LootDropItem(float random, ulong playerId) {
        return [new LootItem {
            ItemName = new DirtItem().name,
            amount = 1,
            Data = new Dictionary<string, object>()
        }];
    }
}