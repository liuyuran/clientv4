using Microsoft.Extensions.Logging;
using ModLoader.logger;

namespace game.scripts.manager;

public class ModManager {
    private readonly ILogger _logger = LogManager.GetLogger<ModManager>();
    public static ModManager instance { get; private set; } = new();
    private const string ModDirectory = "ResourcePack";
    
    public void ScanModPacks() {
        //
    }
    
    private class ModMeta {
        public string displayName { get; init; }
        public string name { get; init; }
        public ulong priority { get; init; }
        public string description { get; init; }
    }
}