using Godot;

namespace game.scripts.start;

public partial class Menu {
    [Export] private PackedScene _singlePlayMenuScene;
    private Control _singlePlayMenu;
    
    private void CloseSinglePlayMenu() {
        if (_singlePlayMenu == null) return;
        _singlePlayMenu.QueueFree();
        _singlePlayMenu = null;
    }
    
    private void OpenSinglePlayMenu() {
        _singlePlayMenu = _singlePlayMenuScene.Instantiate<Control>();
        _modalPanel.AddChild(_singlePlayMenu);
    }
}