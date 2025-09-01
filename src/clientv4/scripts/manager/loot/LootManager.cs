using System;
using System.Collections.Generic;
using System.Linq;
using game.scripts.manager.reset;
using Microsoft.Extensions.Logging;
using ModLoader.handler;
using ModLoader.logger;
using ModLoader.loot;

namespace game.scripts.manager.loot;

/// <summary>
/// Loot config registry and loot action executor.
/// </summary>
public class LootManager : IReset, IDisposable, ILootManager {
    private readonly ILogger _logger = LogManager.GetLogger<LootManager>();
    public static LootManager instance { get; private set; } = new();
    private readonly Random _random = new();
    private readonly Dictionary<string, List<LootConfig>> _lootConfigs = new();

    public void Reset() {
        instance = new LootManager();
        Dispose();
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// registry loot config
    /// </summary>
    /// <typeparam name="T">config define</typeparam>
    public void Register<T>() where T : LootConfig, new() {
        var item = new T();
        var blockName = item.blockName;
        if (!_lootConfigs.ContainsKey(blockName)) {
            _lootConfigs.Add(blockName, []);
        }

        _lootConfigs[blockName].Add(item);
        _logger.LogDebug("Loot config {ItemName} registered", blockName);
    }

    /// <summary>
    /// get loot configs by item name
    /// </summary>
    /// <param name="blockName">target item name</param>
    /// <returns>all loot config about that item</returns>
    public LootConfig[] GetLootConfigs(string blockName) {
        if (!_lootConfigs.TryGetValue(blockName, out var configs)) {
            return [];
        }

        return configs.ToArray();
    }
    
    /// <summary>
    /// get all-loot config 
    /// </summary>
    /// <returns>all loot config</returns>
    public LootConfig[] GetLootConfigs() {
        var configs = new List<LootConfig>();
        foreach (var item in _lootConfigs) {
            configs.AddRange(item.Value);
        }

        return configs.ToArray();
    }

    /// <summary>
    /// loot drop item by block name
    /// </summary>
    /// <param name="blockName">broken block name</param>
    /// <param name="playerId">the user that executes loot action</param>
    /// <returns>loot result</returns>
    public LootItem[] GetLootItems(string blockName, ulong playerId) {
        var configs = GetLootConfigs(blockName);
        var items = new List<LootItem>();
        foreach (var config in configs) {
            items.AddRange(config.LootDropItem(_random.NextSingle(), playerId));
        }

        return items.ToArray();
    }

    /// <summary>
    /// get all loot config can be loot spec item by item name
    /// </summary>
    /// <param name="itemName">target item name</param>
    /// <returns>all loot items about that item</returns>
    public LootConfig[] GetAllCanBeLootItems(string itemName) {
        var items = new List<LootConfig>();
        foreach (var config in GetLootConfigs()) {
            if (config.allDropItem.Contains(itemName)) {
                items.Add(config);
            }
        }

        return items.ToArray();
    }
}