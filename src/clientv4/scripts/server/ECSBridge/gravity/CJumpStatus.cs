using Friflo.Engine.ECS;

namespace game.scripts.server.ECSBridge.gravity;

public struct CJumpStatus: IComponent {
    public ulong LastAction;
    public ulong JumpStartTime;
}