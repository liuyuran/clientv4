using Friflo.Engine.ECS;

namespace game.scripts.server.ECSBridge.sync;

/// <summary>
/// dirt data check component.
/// if entities changed position or rotation, it will be marked as dirty.
/// </summary>
public struct THasDirtData: ITag {
    
}