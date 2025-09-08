using Friflo.Engine.ECS;
using Godot;

namespace game.scripts.server.ECSBridge.render;

/// <summary>
/// information of a single block.
/// </summary>
public struct CBlockInfo : IComponent {
    public ulong BlockId; // block id
    public Vector3I LocalPos; // location in chunk
}