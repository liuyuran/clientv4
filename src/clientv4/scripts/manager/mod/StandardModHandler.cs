using game.scripts.manager.blocks;
using game.scripts.manager.chat;
using game.scripts.manager.item;
using game.scripts.manager.map;
using game.scripts.manager.menu;
using game.scripts.manager.settings;
using ModLoader.handler;

namespace game.scripts.manager.mod;

public class StandardModHandler : IModHandler {
    public IBlockManager GetBlockManager() {
        return BlockManager.instance;
    }

    public IItemManager GetItemManager() {
        return ItemManager.instance;
    }

    public IMapManager GetMapManager() {
        return MapManager.instance;
    }

    public IMenuManager GetMenuManager() {
        return MenuManager.instance;
    }
    
    public ISettingsManager GetSettingsManager() {
        return SettingsManager.instance;
    }
    
    public IChatManager GetChatManager() {
        return ChatManager.instance;
    }
    
    public ICommandManager GetCommandManager() {
        return CommandManager.instance;
    }
}