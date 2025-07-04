using Godot;

namespace game.scripts.start;

public partial class Menu {
    [Export] private PackedScene _singlePlayMenuScene;
    private Panel _singlePlayMenu;
    
    private void CloseSinglePlayMenu() {
        if (_singlePlayMenu != null) {
            _singlePlayMenu.QueueFree();
            _singlePlayMenu = null;
        }
    }
    
    private void OpenSinglePlayMenu() {
        _singlePlayMenu = _singlePlayMenuScene.Instantiate<Panel>();
        AddChild(_singlePlayMenu);
    }
}