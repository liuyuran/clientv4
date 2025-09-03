namespace ModLoader.item.composition;

public static class Block {
    private const string BlockConfigKey = "block";

    public static bool IsBlock(this Item item) {
        return item.Config.ContainsKey(BlockConfigKey);
    }
    
    public static void SetBlock(this Item item, BlockConfig block) {
        item.Config[BlockConfigKey] = block;
    }
    
    public static string GetBlockName(this Item item) {
        if (!item.Config.TryGetValue(BlockConfigKey, out var value)) {
            throw new Exception($"Item {item.name} is not a block");
        }
        
        if (value is BlockConfig config) {
            return config.BlockName;
        }
        
        throw new Exception($"Item {item.name} has invalid block configuration");
    }
    
    public struct BlockConfig(string blockName) {
        public readonly string BlockName = blockName;
    }
}