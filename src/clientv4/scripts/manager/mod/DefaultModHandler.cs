using game.scripts.manager.menu;
using ModLoader.handle;

namespace game.scripts.manager.mod;

public class DefaultModHandler: IModHandler {
    public IMenu menu { get; set; } = new MenuModHandler();
}