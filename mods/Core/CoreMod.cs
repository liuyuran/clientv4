using Microsoft.Extensions.Logging;
using ModLoader;
using ModLoader.handle;
using ModLoader.logger;

namespace Core;

public class CoreMod : IMod {
    private readonly ILogger _logger = LogManager.GetLogger<CoreMod>();

    public void OnLoad(IModHandler handler) {
        handler.menu.AddMenuGroup("core");
        handler.menu.AddMenuItem("core", "core_item", "Core Item", "This is a core item.", () => {
            _logger.LogDebug("Core item clicked!");
        });
        _logger.LogDebug("CoreMod loaded.");
    }

    public void OnUnload(IModHandler handler) {
        _logger.LogDebug("CoreMod unloaded.");
    }

    public void OnGameStart() {
        _logger.LogDebug("Game started.");
    }

    public void OnGameStop() {
        _logger.LogDebug("Game stopped.");
    }
}