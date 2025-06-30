using Godot;

namespace game.unittest;

/// <summary>
/// that unit test node will be shown at runtime.
/// </summary>
public partial class ShownUnitTest: Control {
    private Node _box;
    private RichTextLabel _result;

    public override void _Ready() {
        _box = GetNode<Node>("Box");
        _result = GetNode<RichTextLabel>("Result");
    }
}