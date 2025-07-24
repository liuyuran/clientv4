namespace ModLoader.handler;

public interface IMenuManager {
    public void AddMenuGroup(string id, short order = -1);
    public void AddMenuItem(string groupId, string itemId, string itemName, short order, string description, Action action);
}