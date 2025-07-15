using game.scripts.manager.archive;
using Godot;

namespace game.scripts.start;

public partial class Menu {
    [Export] private PackedScene _singlePlayMenuScene;
    [Export] private PackedScene _singleArchiveItemScene;
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

    private void LoadArchiveList() {
        if (_singlePlayMenu == null) {
            GD.PrintErr("SinglePlayMenu is not initialized.");
            return;
        }
        
        var archiveList = _singlePlayMenu.GetNode<VBoxContainer>("ArchiveList");
        var archiveInfo = _singlePlayMenu.GetNode<RichTextLabel>("ArchiveInfo");
        var loadButton = _singlePlayMenu.GetNode<Button>("LoadButton");
        var deleteButton = _singlePlayMenu.GetNode<Button>("DeleteButton");
        foreach (var child in archiveList.GetChildren()) {
            child.QueueFree();
        }

        var archives = ArchiveManager.instance.List();
        foreach (var archive in archives) {
            var item = _singleArchiveItemScene.Instantiate<HBoxContainer>();
            // TODO inject data to node
            item.Name = archive.Name;
            item.Connect(Control.SignalName.MouseEntered, new Callable(this, MethodName.ArchiveItemMouseEntered));
            item.Connect(Control.SignalName.MouseExited, new Callable(this, MethodName.ArchiveItemMouseExited));
            item.Connect(Control.SignalName.GuiInput, Callable.From(() => {
                ArchiveItemGuiInput(item);
            }));
            archiveList.AddChild(item);
        }
    }
    
    private void ArchiveItemMouseEntered(HBoxContainer item) {
        GD.Print("Mouse entered archive item: ", item.Name);
    }
    
    private void ArchiveItemMouseExited(HBoxContainer item) {
        GD.Print("Mouse exited archive item: ", item.Name);
    }

    private void ArchiveItemGuiInput(HBoxContainer item) {
        // mouse click
        if (Input.IsMouseButtonPressed(MouseButton.Left)) {
            LoadArchiveItemDetailData(item.Name);
        }
    }
    
    private void LoadArchiveItemDetailData(string archiveName) {
        // TODO load archive item detail data
        GD.Print("Loading archive item detail data for: ", archiveName);
    }
}