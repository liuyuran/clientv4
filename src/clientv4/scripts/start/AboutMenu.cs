using System;
using System.Reflection;
using game.scripts.utils;
using Godot;
using ModLoader;

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
        UpdateAboutUITranslate();
    }

    private void UpdateAboutUITranslate() {
        if (_aboutPanel == null) return;
        _backButton.Text = I18N.Tr("core.gui", "about.back");
        _aboutLabel.Text = GetAboutText() + "\n\n";
    }

    private string GetVersion() {
        var buildDate = Assembly.GetExecutingAssembly().GetMetadata("BuildDate");
        if (string.IsNullOrEmpty(buildDate)) {
            buildDate = "unknown";
        } else {
            buildDate = DateTime.TryParse(buildDate, out var dateTime) ? dateTime.ToString("yyyy-MM-dd") : "invalid date format";
        }

        return buildDate;
    }

    private string GetAboutText() {
        var buildDate = Assembly.GetExecutingAssembly().GetMetadata("BuildDate");
        return I18N.Tr("core.gui", "about.text", GetVersion());
    }
}