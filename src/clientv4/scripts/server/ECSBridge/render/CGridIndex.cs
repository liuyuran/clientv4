using Friflo.Engine.ECS;
using Godot;

namespace game.scripts.server.ECSBridge.render;

/// <summary>
/// the index of block for chunk query function.
/// </summary>
public readonly record struct CGridIndex(Vector3I gridPos) : IIndexedComponent<Vector3I> {
    public Vector3I GetIndexedValue() {
        return gridPos;
    }
}