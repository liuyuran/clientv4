using ModLoader.loot;
using ModLoader.skill;

namespace ModLoader.handler;

public interface ILootManager {
    public void Register<T>() where T : LootConfig, new();
    public LootConfig[] GetLootConfigs(string name);
    public LootConfig[] GetLootConfigs();
}