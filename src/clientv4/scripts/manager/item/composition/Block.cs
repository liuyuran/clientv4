namespace game.scripts.manager.item.composition;

public static class Block {
    private const string BlockConfigKey = "block";

    public static bool IsBlock(this Item item) {
        return item.Config.ContainsKey(BlockConfigKey);
    }
    
    public static void SetBlock(this Item item, BlockConfig block) {
        item.Config[BlockConfigKey] = block;
    }
    
    public static ulong GetBlockId(this Item item) {
        if (!item.Config.TryGetValue(BlockConfigKey, out var value)) {
            throw new System.Exception($"Item {item.name} is not a block");
        }
        
        if (value is BlockConfig config) {
            return config.BlockId;
        }
        
        throw new System.Exception($"Item {item.name} has invalid block configuration");
    }
    
    public struct BlockConfig(ulong blockId) {
        public ulong BlockId = blockId;
    }
}