using System;
using System.Collections.Generic;
using Godot;

namespace game.scripts.gui.InGameUI;

public partial class InGamingUI {
    private CanvasLayer _topMenu;
    private Tween _tween;
    private int _menuIndex;
    private CanvasLayer _inventory;
    private readonly List<Action> _menuCallbacks = [];
    
    private void InitializeMenu() {
        _tween = GetTree().CreateTween();
        AddMenuItem(Tr("inventory"), ShowInventory);
    }

    private void SwitchMenu() {
        Visible = !Visible;
        if (Visible) {
            _menuIndex = 0;
            ShowPanelByIndex(-1, _menuIndex);
        }
    }

    private void HideAllSubPanels() {
        HideInventory();
    }
	
    private void ShowPanelByIndex(int from, int to) {
        if (from > -1) {
            if (_tween.IsRunning()) {
                _tween.Stop();
            }
			
            _tween.Play();
        }

        HideAllSubPanels();
        _menuCallbacks[to]?.Invoke();
    }

    private void PanelLeft() {
        if (!Visible) return;
        var prev = _menuIndex;
        _menuIndex = (_menuIndex - 1 + _menuCallbacks.Count) % _menuCallbacks.Count;
        ShowPanelByIndex(prev, _menuIndex);
    }

    private void PanelRight() {
        if (!Visible) return;
        var prev = _menuIndex;
        _menuIndex = (_menuIndex + 1) % _menuCallbacks.Count;
        ShowPanelByIndex(prev, _menuIndex);
    }

    private void AddMenuItem(string name, Action callback) {
        var menuNode = new Button();
        _topMenu.AddChild(menuNode);
        menuNode.Name = name;
        menuNode.Text = name;
        menuNode.ButtonDown += delegate {
            HideAllSubPanels();
            callback();
        };
        _menuCallbacks.Add(callback);
    }
}