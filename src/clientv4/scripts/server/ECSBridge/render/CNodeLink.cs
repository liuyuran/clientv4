using Friflo.Engine.ECS;
using Godot;

namespace game.scripts.server.ECSBridge.render;

public struct CNodeLink : IComponent {
    public MeshInstance3D ClientNode; // 客户端节点，仅渲染
    public StaticBody3D ServerNode; // 服务器节点，仅物理
    public bool Dirty; // 数据已修改，需要重建网格
}