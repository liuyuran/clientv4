using System;
using game.scripts.gui.util;
using game.scripts.manager;
using game.scripts.manager.item;
using game.scripts.manager.player;
using game.scripts.manager.player.settings;
using game.scripts.utils;
using Godot;

namespace game.scripts.gui.InGameUI.component;

[Flags]
public enum ActionBarItemType {
    GamepadActionBar,
    KeyboardActionBar,
    Inventory,
    SkillList
}

public delegate void ActionItemChanged(ActionBarItemType barType);

/// <summary>
/// will bind to action bar item node.
/// which minimally unit to show an item or action in the action bar.
/// it can be dragged and dropped to change action bar settings.
/// well, just do not want to find a node in the whole action bar node, too slowly.
/// </summary>
public partial class ActionBarItem : Control {
    public static event ActionItemChanged OnItemChanged;
    private ActionItem? _config;
    private int _index;
    private ActionBarItemType _type;

    public void SetItem(ActionBarItemType type, ActionItem configItem, int index, int count) {
        _config = configItem;
        _index = index;
        _type = type;
        var icon = this.FindNodeByName<TextureRect>("ClickArea");
        if (icon == null) return;
        // set icon
        switch (configItem.Type) {
            case ActionItemType.Item:
                icon.Texture = MaterialManager.instance.GetItemTexture(configItem.Id);
                break;
            case ActionItemType.Skill:
                break;
            case ActionItemType.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override Variant _GetDragData(Vector2 atPosition) {
        if (_config == null) return new Variant();
        var dragInfo = new ActionBarDragInfo {
            Type = _config.Value.Type,
            FromType = _type,
            FromPosition = _index
        };
        SetDragPreview(this.Duplicate() as Control);
        return dragInfo;
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data) {
        if (data.AsGodotObject() is not ActionBarDragInfo dragInfo) return false;
        if (!_config.HasValue) return false;
        // disable drop to the action list
        if (_type == ActionBarItemType.SkillList) return false;
        // disable drop between the inventory and action list
        if ((dragInfo.FromType & _type) == (ActionBarItemType.Inventory & ActionBarItemType.SkillList)) return false;
        // disable drops from the gamepad/keyboard action bar to the inventory/action list
        var fromActionBar = dragInfo.FromType is ActionBarItemType.GamepadActionBar or ActionBarItemType.KeyboardActionBar;
        var toInventoryOrActionList = _type is ActionBarItemType.Inventory or ActionBarItemType.SkillList;
        if (fromActionBar && toInventoryOrActionList) return false;
        return true;
    }

    public override void _DropData(Vector2 atPosition, Variant data) {
        if (data.AsGodotObject() is not ActionBarDragInfo dragInfo) return;
        if (!_config.HasValue) return;
        switch (_type) {
            case ActionBarItemType.GamepadActionBar: {
                // drop to the gamepad action bar
                var gamepadConfig = PlayerSettingsManager.instance.GetSettings().ActionBar.GamePadItems;
                switch (dragInfo.FromType) {
                    case ActionBarItemType.GamepadActionBar: {
                        (gamepadConfig[dragInfo.FromPosition], gamepadConfig[_index]) = (gamepadConfig[_index], gamepadConfig[dragInfo.FromPosition]);
                        break;
                    }
                    case ActionBarItemType.KeyboardActionBar: {
                        var keyboardConfig = PlayerSettingsManager.instance.GetSettings().ActionBar.KeyboardItems;
                        (keyboardConfig[dragInfo.FromPosition], gamepadConfig[_index]) = (gamepadConfig[_index], keyboardConfig[dragInfo.FromPosition]);
                        break;
                    }
                    case ActionBarItemType.Inventory: {
                        var playerId = PlayerManager.instance.GetCurrentPlayerId();
                        var inventory = InventoryManager.instance.GetInventoryItems(playerId);
                        var item = inventory[dragInfo.FromPosition];
                        var itemName = item.name;
                        var itemId = ItemManager.instance.GetItemId(itemName);
                        gamepadConfig[_index] = new ActionItem {
                            Type = ActionItemType.Item,
                            Id = itemId
                        };
                        break;
                    }
                    case ActionBarItemType.SkillList:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                break;
            }
            case ActionBarItemType.KeyboardActionBar: {
                // drop to the keyboard action bar
                var keyboardConfig = PlayerSettingsManager.instance.GetSettings().ActionBar.KeyboardItems;
                switch (dragInfo.FromType) {
                    case ActionBarItemType.GamepadActionBar: {
                        var gamepadConfig = PlayerSettingsManager.instance.GetSettings().ActionBar.GamePadItems;
                        (gamepadConfig[dragInfo.FromPosition], keyboardConfig[_index]) = (keyboardConfig[_index], gamepadConfig[dragInfo.FromPosition]);
                        break;
                    }
                    case ActionBarItemType.KeyboardActionBar: {
                        (keyboardConfig[dragInfo.FromPosition], keyboardConfig[_index]) = (keyboardConfig[_index], keyboardConfig[dragInfo.FromPosition]);
                        break;
                    }
                    case ActionBarItemType.Inventory: {
                        var playerId = PlayerManager.instance.GetCurrentPlayerId();
                        var inventory = InventoryManager.instance.GetInventoryItems(playerId);
                        var item = inventory[dragInfo.FromPosition];
                        var itemName = item.name;
                        var itemId = ItemManager.instance.GetItemId(itemName);
                        keyboardConfig[_index] = new ActionItem {
                            Type = ActionItemType.Item,
                            Id = itemId
                        };
                        break;
                    }
                    case ActionBarItemType.SkillList:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                break;
            }
            case ActionBarItemType.Inventory: {
                // swap the inner item of inventory
                if (dragInfo.FromType != ActionBarItemType.Inventory) return;
                var playerId = PlayerManager.instance.GetCurrentPlayerId();
                var inventory = InventoryManager.instance.GetInventoryItems(playerId);
                var fromIndex = dragInfo.FromPosition;
                var toIndex = _index;
                (inventory[fromIndex], inventory[toIndex]) = (inventory[toIndex], inventory[fromIndex]);
                break;
            }
            case ActionBarItemType.SkillList:
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }
        OnItemChanged?.Invoke(_type);
        if (_type != dragInfo.FromType) {
            OnItemChanged?.Invoke(dragInfo.FromType);
        }
    }
}