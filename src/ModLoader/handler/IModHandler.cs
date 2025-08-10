using ModLoader.scene;

namespace ModLoader.handler;

public interface IModHandler {
    public IArchiveManager GetArchiveManager();
    public IBlockManager GetBlockManager();
    public IItemManager GetItemManager();
    public IMapManager GetMapManager();
    public IMenuManager GetMenuManager();
    public ISettingsManager GetSettingsManager();
    public IChatManager GetChatManager();
    public ICommandManager GetCommandManager();
    public IRecipeManager GetRecipeManager();
    public ISkillManager GetSkillManager();
    public ISceneManager GetSceneManager();
}