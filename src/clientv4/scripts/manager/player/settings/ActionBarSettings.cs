using System.Collections.Generic;

namespace game.scripts.manager.player.settings;

public enum ActionBarMode {
    Keyboard,
    Gamepad
}

public enum ActionItemType {
    Item
}

public struct ActionItem {
    public ActionItemType Type;
    public ulong Id;
}

public struct ActionBarSettings {
    public ActionBarMode Mode;
    public List<ActionItem> KeyboardItems;
    public List<ActionItem> GamePadItems;
}