using Friflo.Engine.ECS;

namespace game.scripts.server.ECSBridge.block;

public struct CDigProgress: IComponent {
    public ulong MaxProgress;
    public ulong Progress;
}