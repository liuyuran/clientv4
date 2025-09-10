using Friflo.Engine.ECS;
using Godot;

namespace game.scripts.server.ECSBridge.render;

/// <summary>
/// the index of block for chunk query function.
/// </summary>
public readonly record struct CGridIndex(int worldId, Vector3I gridPos) : IIndexedComponent<Vector4I> {
    public Vector4I GetIndexedValue() {
        return new Vector4I(worldId, gridPos.X, gridPos.Y, gridPos.Z);
    }
}