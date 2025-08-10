using game.scripts.manager.archive;
using game.scripts.manager.blocks;
using game.scripts.manager.chat;
using game.scripts.manager.item;
using game.scripts.manager.map;
using game.scripts.manager.menu;
using game.scripts.manager.recipe;
using game.scripts.manager.scene;
using game.scripts.manager.settings;
using game.scripts.manager.skill;
using ModLoader.handler;
using ModLoader.scene;

namespace game.scripts.manager.mod;

public class StandardModHandler : IModHandler {
    public IArchiveManager GetArchiveManager() {
        return ArchiveManager.instance;
    }
    
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

    public IRecipeManager GetRecipeManager() {
        return RecipeManager.instance;
    }

    public ISkillManager GetSkillManager() {
        return SkillManager.instance;
    }

    public ISceneManager GetSceneManager() {
        return SceneManager.instance;
    }
}