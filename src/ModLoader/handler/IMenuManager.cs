using ModLoader.setting;

namespace ModLoader.handler;

public interface IMenuManager {
    public void AddMenuGroup(string id, int order = -1);
    public void AddMenuItem(string groupId, string itemId, GetString itemName, int order, GetString description, Action action);
}