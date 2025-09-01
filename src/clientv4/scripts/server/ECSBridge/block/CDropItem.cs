using Friflo.Engine.ECS;

namespace game.scripts.server.ECSBridge.block;

public struct CDropItem: IComponent {
    public ulong PlayerId;
    public ulong BlockId;
}