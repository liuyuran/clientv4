namespace ModLoader.handler;

public interface IModHandler {
    public IBlockManager GetBlockManager();
    public IItemManager GetItemManager();
    public IMapManager GetMapManager();
    public IMenuManager GetMenuManager();
}