using System;
using System.Reflection;
using game.scripts.utils;
using Godot;

namespace game.scripts.start;

public partial class Menu {
    [Export] private PackedScene _aboutPanelScene;
    private Control _aboutPanel;
    private RichTextLabel _aboutLabel;
    private Button _backButton;

    private void CloseAboutPanel() {
        if (_aboutPanel == null) return;
        _aboutPanel.QueueFree();
        _aboutPanel = null;
        _modalPanel.Visible = false;
    }

    private void OpenAboutPanel() {
        _aboutPanel = _aboutPanelScene.Instantiate<Control>();
        _modalPanel.AddChild(_aboutPanel);
        _aboutLabel = _aboutPanel.FindNodeByName<RichTextLabel>("Text");
        _backButton = _aboutPanel.FindNodeByName<Button>("Back");
        _backButton.Pressed += CloseAboutPanel;
        _aboutLabel.Text = GetAboutText() + "\n\n";
    }

    private string GetAboutText() {
        var buildDate = Assembly.GetExecutingAssembly().GetMetadata("BuildDate");
        // parse time stamp to date time.
        if (string.IsNullOrEmpty(buildDate)) {
            buildDate = "unknown";
        } else {
            buildDate = DateTime.TryParse(buildDate, out var dateTime) ? dateTime.ToString("yyyy-MM-dd") : "invalid date format";
        }
        return "This is a demo game for Godot 4.2. Build in " + buildDate;
    }
}