using game.scripts.gui.InGameUI.component;
using game.scripts.manager.player.settings;
using Godot;

namespace game.scripts.gui.util;

public partial class ActionBarDragInfo: GodotObject {
    public ActionItemType Type;
    public ActionBarItemType FromType;
    public int FromPosition;
}