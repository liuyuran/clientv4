using System;
using ModLoader.handle;

namespace game.scripts.manager.menu;

public class MenuModHandler: IMenu {
    public void AddMenuGroup(string groupId, short order = -1) {
        MenuManager.instance.AddMenuGroup(groupId, order);
    }

    public void AddMenuItem(string groupId, string itemId, string itemName, string itemDescription, Action action, short order = -1) {
        MenuManager.instance.AddMenuItem(groupId, itemId, itemName, order, itemDescription, action);
    }
}