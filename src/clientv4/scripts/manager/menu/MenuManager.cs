using System;
using System.Collections.Generic;
using game.scripts.manager.menu.controller;
using game.scripts.manager.mod;
using game.scripts.manager.reset;
using game.scripts.manager.scene;
using game.scripts.utils;
using Godot;
using ModLoader;
using ModLoader.handler;
using ModLoader.setting;

namespace game.scripts.manager.menu;

public class MenuManager: IReset, IDisposable, IMenuManager {
    public static MenuManager instance { get; private set; } = new();
    private List<MenuGroupItem> _menus = [];

    private MenuManager() {
        AddMenuGroup("character", 0);
        AddMenuItem("character", "inventory",
            () => I18N.Tr("core", "menu.character.inventory"), 1,
            () => I18N.Tr("core", "menu.character.inventory.desc"), () => {
                SceneManager.instance.OpenSceneModal("res://prefabs/gui/inventory.tscn", new InventoryController());
            });
        AddMenuGroup("system", int.MaxValue);
        AddMenuItem("system", "back-to-start",
            () => I18N.Tr("core", "menu.system.back-to-start"), 1,
            () => I18N.Tr("core", "menu.system.back-to-start.desc"), () => {
                ModManager.instance.OnStopGame();
                GameNodeReference.CurrentScene.GetTree().ChangeSceneToPacked(GameNodeReference.StartScenePacked);
            });
        AddMenuItem("system", "exit",
            () => I18N.Tr("core", "menu.system.exit"), 1,
            () => I18N.Tr("core", "menu.system.exit.desc"), () => {
                GameNodeReference.CurrentScene.GetTree().Quit();
            });
    }
    
    public void AddMenuGroup(string id, int order = -1) {
        var group = new MenuGroupItem {
            Id = id,
            Children = [],
            ListOrder = order >= 0 ? order : _menus.Count
        };
        
        _menus.Add(group);
    }
    
    public void AddMenuItem(string groupId, string itemId, GetString itemName, int order, GetString description, Action action) {
        var group = _menus.Find(g => g.Id == groupId);
        if (group.Id == null) {
            GD.PrintErr($"Menu group {groupId} not found.");
            return;
        }
        
        var item = new MenuItem {
            Id = itemId,
            Name = itemName,
            Action = action,
            ListOrder = order >= 0 ? order : group.Children.Count,
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
        public int ListOrder;
    }
    
    public struct MenuItem {
        public string Id;
        public GetString Name;
        public Action Action;
        public int ListOrder;
        public GetString Description;
    }

    public void Reset() {
        instance = new MenuManager();
        Dispose();
    }
    public void Dispose() {
        _menus.Clear();
        GC.SuppressFinalize(this);
    }
}