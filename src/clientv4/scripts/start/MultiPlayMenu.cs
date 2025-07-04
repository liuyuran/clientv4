using Godot;

namespace game.scripts.start;

public partial class Menu {
    [Export] private PackedScene _multiPlayMenuScene;
    private Panel _multiPlayMenu;
    
    private void CloseMultiPlayMenu() {
        if (_multiPlayMenu != null) {
            _multiPlayMenu.QueueFree();
            _multiPlayMenu = null;
        }
    }
    
    private void OpenMultiPlayMenu() {
        _multiPlayMenu = _multiPlayMenuScene.Instantiate<Panel>();
        AddChild(_multiPlayMenu);
    }
}