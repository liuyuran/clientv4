using Core.terrain.generator;
using game.scripts.manager.map;
using game.scripts.manager.menu;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using ModLoader;
using ModLoader.logger;

namespace Core;

[UsedImplicitly]
public class CoreMod : IMod {
    private readonly ILogger _logger = LogManager.GetLogger<CoreMod>();

    public void OnLoad() {
        AddTerrain();
        AddMenu();
        _logger.LogDebug(I18N.Tr("mod.core", "mod.loaded"));
    }

    private void AddTerrain() {
        MapManager.instance.RegisterGenerator<StandardWorldGenerator>(0);
    }

    private void AddMenu() {
        MenuManager.instance.AddMenuGroup("core", -1);
        MenuManager.instance.AddMenuItem("core", "core mod settings",
            I18N.Tr("mod.core", "menu.setting"),
            -1,
            I18N.Tr("mod.core", "menu.setting.desc"),
            () => { _logger.LogDebug("Core item clicked!"); });
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