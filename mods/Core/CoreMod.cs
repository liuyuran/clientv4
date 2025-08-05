using Core.block;
using Core.item;
using Core.terrain.generator;
using Microsoft.Extensions.Logging;
using ModLoader;
using ModLoader.handler;
using ModLoader.logger;

namespace Core;

public class CoreMod : IMod {
    private readonly ILogger _logger = LogManager.GetLogger<CoreMod>();
    public static IModHandler? Handler;

    public void OnLoad(IModHandler handler) {
        Handler = handler;
        AddBlock();
        AddItem();
        AddTerrain();
        AddMenu();
        _logger.LogDebug("{message}", I18N.Tr("mod.core", "mod.loaded"));
    }

    private void AddTerrain() {
        if (Handler == null) {
            _logger.LogError("Handler is null, cannot register terrain generator.");
            return;
        }
        Handler.GetMapManager().RegisterGenerator<StandardWorldGenerator>(0);
    }

    private void AddMenu() {
        if (Handler == null) {
            _logger.LogError("Handler is null, cannot register menu.");
            return;
        }
        Handler.GetMenuManager().AddMenuGroup("core");
        Handler.GetMenuManager().AddMenuItem("core", "core mod settings",
            () => I18N.Tr("mod.core", "menu.setting"),
            -1,
            () => I18N.Tr("mod.core", "menu.setting.desc"),
            () => { _logger.LogDebug("Core item clicked!"); });
    }

    private void AddBlock() {
        if (Handler == null) {
            _logger.LogError("Handler is null, cannot register blocks.");
            return;
        }

        Handler.GetBlockManager().Register<Water>();
        Handler.GetBlockManager().Register<Dirt>();
        Handler.GetBlockManager().Register<Stone>();
    }

    private void AddItem() {
        if (Handler == null) {
            _logger.LogError("Handler is null, cannot register items.");
            return;
        }
        Handler.GetItemManager().Register<DirtItem>();
    }

    public void OnUnload() {
        _logger.LogDebug("CoreMod unloaded.");
    }

    public void OnGameStart() {
        _logger.LogDebug("Game started.");
    }

    public void OnGameStop() {
        _logger.LogDebug("Game stopped.");
    }
}