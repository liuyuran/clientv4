using Godot;

namespace game.scripts;

public partial class RenderTest: Node3D {
    [Export] private Node3D _prototype;
    private double _startTime;
    private double _endTime;

    public override void _Process(double delta) {
        const int size = 48;
        const int totalCount = size * size * size;
        
        _startTime = Time.GetTicksMsec();
        
        for (var x = 0; x < size; x++) {
            for (var y = 0; y < size; y++) {
                for (var z = 0; z < size; z++) {
                    var instance = (Node3D)_prototype.Duplicate();
                    AddChild(instance);
                    instance.Position = new Vector3(x, y, z);
                }
            }
        }
        
        _endTime = Time.GetTicksMsec();
        var renderTime = _endTime - _startTime;
        GD.Print($"Rendered {totalCount} instances in {renderTime}ms");
        SetProcess(false);
    }
}