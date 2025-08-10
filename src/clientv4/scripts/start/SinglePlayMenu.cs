using game.scripts.manager.archive;
using game.scripts.manager.map;
using game.scripts.manager.reset;
using game.scripts.utils;
using Godot;
using Microsoft.Extensions.Logging;
using ModLoader;

namespace game.scripts.start;

/// <summary>
/// single play menu for managing local game archives.
/// </summary>
public partial class Menu {
    [Export] private PackedScene _singlePlayMenuScene;
    [Export] private PackedScene _singleArchiveItemScene;
    private Control _singlePlayMenu;
    private Control _createPanel;
    private string _selectedArchiveName = string.Empty;

    /// <summary>
    /// close the single play menu if it is open.
    /// </summary>
    private void CloseSinglePlayMenu() {
        if (_singlePlayMenu == null) return;
        _singlePlayMenu.QueueFree();
        _singlePlayMenu = null;
        _modalPanel.Visible = false;
    }

    /// <summary>
    /// open the single play menu, if it is not already open.
    /// </summary>
    private void OpenSinglePlayMenu() {
        if (_singlePlayMenu != null) {
            _logger.LogWarning("SinglePlayMenu is already open.");
            return;
        }

        _singlePlayMenu = _singlePlayMenuScene.Instantiate<Control>();
        _modalPanel.AddChild(_singlePlayMenu);
        LoadArchiveList();
        UpdateSinglePlayUITranslate();
    }

    private void UpdateSinglePlayUITranslate() {
        if (_singlePlayMenu == null) return;
        var createButton = _singlePlayMenu.FindNodeByName<Button>("CreateWorld");
        var createCancelButton = _createPanel.FindNodeByName<Button>("CreateCancelButton");
        var loadButton = _singlePlayMenu.FindNodeByName<Button>("LoadButton");
        var deleteButton = _singlePlayMenu.FindNodeByName<Button>("DeleteButton");
        createButton.Text = I18N.Tr("core.gui", "single-play.create-world");
        createCancelButton.Text = I18N.Tr("core.gui", "single-play.create-world.cancel");
        loadButton.Text = I18N.Tr("core.gui", "single-play.load");
        deleteButton.Text = I18N.Tr("core.gui", "single-play.delete");
    }

    /// <summary>
    /// load the archive list into the single play menu.
    /// </summary>
    private void LoadArchiveList() {
        if (_singlePlayMenu == null) {
            _logger.LogError("SinglePlayMenu is not initialized.");
            return;
        }

        // bind action for buttons.
        _createPanel = _singlePlayMenu.FindNodeByName<Control>("CreateWorldPanel");
        var archiveList = _singlePlayMenu.FindNodeByName<VBoxContainer>("ArchiveList");
        var createButton = _singlePlayMenu.FindNodeByName<Button>("CreateWorld");
        var createCancelButton = _createPanel.FindNodeByName<Button>("CreateCancelButton");
        var loadButton = _singlePlayMenu.FindNodeByName<Button>("LoadButton");
        var deleteButton = _singlePlayMenu.FindNodeByName<Button>("DeleteButton");

        loadButton.Pressed += () => {
            if (string.IsNullOrEmpty(_selectedArchiveName)) {
                _logger.LogWarning("No archive selected for loading.");
                return;
            }

            ResetManager.Reset();
            ArchiveManager.instance.Load(_selectedArchiveName);
            JumpToGameSceneAndStartLocalServer();
        };

        deleteButton.Pressed += () => {
            if (string.IsNullOrEmpty(_selectedArchiveName)) {
                GD.PrintErr("No archive selected for deletion.");
                return;
            }

            DeleteArchiveItem(_selectedArchiveName);
            LoadArchiveList();
        };

        createButton.Pressed += OpenSingleCreateWorld;
        createCancelButton.Pressed += CloseSingleCreateWorld;

        // clean and load archives from ArchiveManager, and display simple info in the archive list.
        foreach (var child in archiveList.GetChildren()) {
            child.QueueFree();
        }
        
        var archives = ArchiveManager.instance.List();
        foreach (var archive in archives) {
            var item = _singleArchiveItemScene.Instantiate<HBoxContainer>();
            var displayLabel = item.FindNodeByName<RichTextLabel>("DisplayName");
            item.Name = archive.Name;
            displayLabel.Text = archive.Name;
            item.MouseFilter = MouseFilterEnum.Pass;
            item.Connect(Control.SignalName.MouseEntered, Callable.From(() => { ArchiveItemMouseEntered(item); }));
            item.Connect(Control.SignalName.MouseExited, Callable.From(() => { ArchiveItemMouseExited(item); }));
            // connect callable should have the same parameter type and sum as the override signal usage.
            item.Connect(Control.SignalName.GuiInput, Callable.From((InputEvent @event) => { ArchiveItemGuiInput(item); }));
            archiveList.AddChild(item);
        }

        // auto select the first archive item if exists and load its detail data.
        if (archiveList.GetChildCount() > 0) {
            var firstItem = archiveList.GetChild<HBoxContainer>(0);
            _selectedArchiveName = firstItem.Name;
            LoadArchiveItemDetailData(_selectedArchiveName);
        } else {
            _selectedArchiveName = string.Empty;
        }
    }

    /// <summary>
    /// mouse in hover event for archive item.
    /// </summary>
    private void ArchiveItemMouseEntered(HBoxContainer item) {
        GD.Print("Mouse entered archive item: ", item.Name);
    }

    /// <summary>
    /// mouse out hover event for archive item.
    /// </summary>
    private void ArchiveItemMouseExited(HBoxContainer item) {
        GD.Print("Mouse exited archive item: ", item.Name);
    }

    /// <summary>
    /// click event for archive item.
    /// </summary>
    private void ArchiveItemGuiInput(HBoxContainer item) {
        // mouse click
        if (Input.IsMouseButtonPressed(MouseButton.Left)) {
            LoadArchiveItemDetailData(item.Name);
        }
    }

    /// <summary>
    /// load the detail data for the selected archive item.
    /// </summary>
    private void LoadArchiveItemDetailData(string archiveName) {
        // TODO load archive item detail data
        var archiveInfo = _singlePlayMenu.FindNodeByName<RichTextLabel>("ArchiveInfo");
        archiveInfo.Text = $"Loading details for archive: {archiveName}";
        _logger.LogDebug("Loading archive item detail data for: {name}", archiveName);
    }

    /// <summary>
    /// callback for deleting an archive item.
    /// </summary>
    private void DeleteArchiveItem(string archiveName) {
        // TODO delete archive item
        _logger.LogDebug("Deleting archive item: {name}", archiveName);
    }

    /// <summary>
    /// open the single play menu and load the archive list.
    /// </summary>
    private void OpenSingleCreateWorld() {
        _createPanel.Visible = true;
        var seed = _createPanel.FindNodeByName<LineEdit>("SeedInput");
        var createButton = _createPanel.FindNodeByName<Button>("StartGame");
        seed.Text = "";
        createButton.Pressed += CreateArchive;
    }

    /// <summary>
    /// close the single create-world panel and reset the input fields.
    /// </summary>
    private void CloseSingleCreateWorld() {
        _createPanel.Visible = false;
        var seed = _createPanel.FindNodeByName<LineEdit>("SeedInput");
        var createButton = _createPanel.FindNodeByName<Button>("StartGame");
        seed.Text = "";
        createButton.Pressed -= CreateArchive;
    }

    /// <summary>
    /// create a new archive with the specified seed and start the game.
    /// </summary>
    private void CreateArchive() {
        var seed = _createPanel.FindNodeByName<LineEdit>("SeedInput");
        MapManager.Seed = long.Parse(seed.Text);
        ResetManager.Reset();
        ArchiveManager.instance.Create("new world");
        JumpToGameSceneAndStartLocalServer();
    }
}