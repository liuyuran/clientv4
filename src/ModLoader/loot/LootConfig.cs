namespace ModLoader.loot;

public abstract class LootConfig {
    public virtual string blockName => throw new System.NotImplementedException();
    public virtual string[] allDropItem => throw new System.NotImplementedException();
    public abstract LootItem[] LootDropItem(float random, ulong playerId);
}