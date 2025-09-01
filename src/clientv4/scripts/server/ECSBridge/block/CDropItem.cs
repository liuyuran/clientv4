using Friflo.Engine.ECS;
using Godot;

namespace game.scripts.server.ECSBridge.block;

public struct CDropItem: IComponent {
    public ulong PlayerId;
    public ulong BlockId;
}