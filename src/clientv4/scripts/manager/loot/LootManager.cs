using Microsoft.Extensions.Logging;
using ModLoader.logger;

namespace game.scripts.manager.loot;

public class LootManager {
    private readonly ILogger _logger = LogManager.GetLogger<LootManager>();
    public static LootManager instance { get; private set; } = new();
}