using System;
using System.Collections.Generic;
using game.scripts.config;
using Godot;

namespace game.unittest;

/// <summary>
/// that unit test node will be shown at runtime.
/// </summary>
public partial class ShownUnitTest: Control {
    private Node _box;
    private Control _boxUI;
    private Node3D _boxGame;
    private ScrollContainer _result;
    private VBoxContainer _logBox;
    private readonly List<Action> _test = [];
    private readonly List<Action> _testClean = [];
    private int _currentTestIndex;

    public override void _Ready() {
        _box = GetNode<Node>("Box");
        _boxUI = new Control();
        _boxGame = new Node3D();
        _box.AddChild(_boxUI);
        _box.AddChild(_boxGame);
        _boxUI.Visible = false;
        _boxGame.Visible = false;
        _result = GetNode<ScrollContainer>("Result");
        _logBox = new VBoxContainer();
        _result.AddChild(_logBox);
        _result.VerticalScrollMode = ScrollContainer.ScrollMode.ShowAlways;
        _result.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        CallDeferred(MethodName.OnRootSizeChanged);
        GetTree().Root.SizeChanged += OnRootSizeChanged;
        AddTest(RunTestForECSBridge, CleanupECSBridgeTest);
    }

    private void AddTest(Action main, Action clean) {
        _test.Add(main);
        _testClean.Add(clean);
    }

    private void OnRootSizeChanged() {
        _result.Size = GetTree().Root.Size - new Vector2I(20, 0);
        _result.Position = new Vector2(10, 0);
        for (var i = 0; i < _result.GetChildCount(); i++) {
            var child = _result.GetChild(i);
            if (child is RichTextLabel label) {
                label.CustomMinimumSize = new Vector2(_result.Size.X, 30);
            }
        }
    }
    
    private void AppendLog(string message) {
        var label = new RichTextLabel {
            Text = message,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            FitContent = true,
            BbcodeEnabled = true,
            CustomMinimumSize = new Vector2(_result.Size.X, 30),
            AutowrapMode = TextServer.AutowrapMode.Word,
        };
        _logBox.AddChild(label);
        _logBox.CustomMinimumSize = _logBox.Size += new Vector2(0, label.Size.Y);
        _result.ScrollVertical = (int)_logBox.CustomMinimumSize.Y;
    }

    public override void _Input(InputEvent @event) {
        InputManager.instance.HandleInputEvent(@event);
    }

    public override void _Process(double delta) {
        var wheel = InputManager.instance.GetMouseWheelAndReset();
        if (wheel != 0) {
            _result.ScrollVertical += (int)(wheel * 10);
        }

        if (_currentTestIndex >= _test.Count) {
            return;
        }
        
        _test[_currentTestIndex]?.Invoke();
        _testClean[_currentTestIndex]?.Invoke();

        _currentTestIndex++;
    }
}