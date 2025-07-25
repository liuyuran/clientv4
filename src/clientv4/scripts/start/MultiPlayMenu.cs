using Godot;

namespace game.scripts.start;

public partial class Menu {
    [Export] private PackedScene _multiPlayMenuScene;
    private Control _multiPlayMenu;
    
    private void CloseMultiPlayMenu() {
        if (_multiPlayMenu == null) return;
        _multiPlayMenu.QueueFree();
        _multiPlayMenu = null;
        _modalPanel.Visible = false;
    }
    
    private void OpenMultiPlayMenu() {
        _multiPlayMenu = _multiPlayMenuScene.Instantiate<Control>();
        _modalPanel.AddChild(_multiPlayMenu);
    }
}