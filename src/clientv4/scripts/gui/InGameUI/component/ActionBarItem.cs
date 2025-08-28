using System;
using game.scripts.gui.util;
using game.scripts.manager;
using game.scripts.manager.player.settings;
using game.scripts.utils;
using Godot;

namespace game.scripts.gui.InGameUI.component;

public enum ActionBarItemType {
    GamepadActionBar,
    KeyboardActionBar, 
    Inventory
}

/// <summary>
/// will bind to action bar item node.
/// which minimally unit to show a item or action in action bar.
/// it can be dragged and dropped to change action bar settings.
/// well, just not want to find node in whole action bar node, too slow.
/// </summary>
public partial class ActionBarItem: Control {
    private ActionItem? _config;
    private int _index;
    private ActionBarItemType _type;
    
    public void SetItem(ActionBarItemType type, ActionItem configItem, int index) {
        _config = configItem;
        _index = index;
        _type = type;
        var icon = this.FindNodeByName<TextureRect>("ClickArea");
        if (icon == null) return;
        icon.Texture = configItem.Type switch {
            ActionItemType.Item => MaterialManager.instance.GetItemTexture(configItem.Id),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public override Variant _GetDragData(Vector2 atPosition) {
        if (_config == null) return new Variant();
        var dragInfo = new ActionBarDragInfo {
            Type = _config.Value.Type,
            FromPosition = (ulong) GetIndex()
        };
        SetDragPreview(this.Duplicate() as Control);
        return dragInfo;
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data) {
        if (data.AsGodotObject() is not ActionBarDragInfo) return false;
        if (!_config.HasValue) return false;
        return true;
    }

    public override void _DropData(Vector2 atPosition, Variant data) {
        if (data.AsGodotObject() is not ActionBarDragInfo dragInfo) return;
        if (!_config.HasValue) return;
    }
}