namespace ModLoader.handle;

public interface IMenu {
    public void AddMenuGroup(string groupId, short order = -1);
    
    public void AddMenuItem(string groupId, string itemId, string itemName, string itemDescription, Action action, short order = -1);
}