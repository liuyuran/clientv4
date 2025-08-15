using System.Collections.Generic;

namespace game.scripts.manager.player.settings;

public enum ActionBarMode {
    Keyboard,
    Gamepad
}

public struct ActionItem {
    public string Type;
    public string Id;
}

public struct ActionBarSettings {
    public ActionBarMode Mode;
    public List<ActionItem> KeyboardItems;
    public List<ActionItem> GamePadItems;
}