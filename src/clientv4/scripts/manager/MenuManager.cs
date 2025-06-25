using System;
using System.Collections.Generic;
using Godot;

namespace game.scripts.manager;

public class MenuManager {
    public static MenuManager instance { get; private set; } = new();
    private List<MenuGroupItem> _menus = [];
    
    public void AddMenuGroup(string id, short order) {
        var group = new MenuGroupItem {
            Id = id,
            Children = [],
            ListOrder = order
        };
        
        _menus.Add(group);
    }
    
    public void AddMenuItem(string groupId, string itemId, string itemName, short order, string description, Action action) {
        var group = _menus.Find(g => g.Id == groupId);
        if (group.Id == null) {
            GD.PrintErr($"Menu group {groupId} not found.");
            return;
        }
        
        var item = new MenuItem {
            Id = itemId,
            Name = itemName,
            Action = action,
            ListOrder = order,
            Description = description
        };
        
        group.Children.Add(item);
    }

    public void RemoveMenuItem(string groupId, string itemId) {
        var group = _menus.Find(g => g.Id == groupId);
        if (group.Id == null) {
            GD.PrintErr($"Menu group {groupId} not found.");
            return;
        }
        
        var item = group.Children.Find(i => i.Id == itemId);
        if (item.Id == null) {
            GD.PrintErr($"Menu item {itemId} not found in group {groupId}.");
            return;
        }
        
        group.Children = group.Children.FindAll(i => i.Id != itemId);
    }
    
    public void RemoveMenuGroup(string groupId) {
        var group = _menus.Find(g => g.Id == groupId);
        if (group.Id == null) {
            GD.PrintErr($"Menu group {groupId} not found.");
            return;
        }
        
        _menus = _menus.FindAll(g => g.Id != groupId);
    }
    
    public MenuItem[][] GetMenuGroups() {
        var menus = _menus.FindAll(g => g.Children.Count > 0);
        menus.Sort((g1, g2) => g1.ListOrder.CompareTo(g2.ListOrder));
        var result = new MenuItem[menus.Count][];
        for (var i = 0; i < menus.Count; i++) {
            var group = menus[i];
            result[i] = group.Children.ToArray();
            Array.Sort(result[i], (a, b) => a.ListOrder.CompareTo(b.ListOrder));
        }
        return result;
    }

    private struct MenuGroupItem {
        public string Id;
        public List<MenuItem> Children;
        public short ListOrder;
    }
    
    public struct MenuItem {
        public string Id;
        public string Name;
        public Action Action;
        public short ListOrder;
        public string Description;
    }
}