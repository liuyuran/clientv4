using game.scripts.manager.blocks;
using game.scripts.manager.chat;
using game.scripts.manager.item;
using game.scripts.manager.map;
using game.scripts.manager.menu;
using game.scripts.manager.mod;
using game.scripts.manager.player;
using game.scripts.utils;

namespace game.scripts.manager.reset;

public class ResetManager {
    public void Reset() {
        PlayerManager.instance.Reset();
        MenuManager.instance.Reset();
        MapManager.instance.Reset();
        ChatManager.instance.Reset();
        ItemManager.instance.Reset();
        BlockManager.instance.Reset();
        ModManager.instance.Reset();
        LanguageManager.instance.Reset();
        MaterialManager.instance.Reset();
        ResourcePackManager.instance.Reset();
        GameNodeReference.UI = null;
        GameStatus.SetStatus(GameStatus.Status.StartMenu);
    }
}