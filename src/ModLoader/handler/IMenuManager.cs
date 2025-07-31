using ModLoader.setting;

namespace ModLoader.handler;

public interface IMenuManager {
    public void AddMenuGroup(string id, short order = -1);
    public void AddMenuItem(string groupId, string itemId, GetString itemName, short order, GetString description, Action action);
}