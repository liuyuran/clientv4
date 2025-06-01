using Friflo.Engine.ECS;
using Godot;

namespace game.scripts.server.ECSBridge.input;

public struct CTransform: IComponent {
    public Vector3 Position;
    public Vector3 Rotation;
    public Vector3 BasisX;
    public Vector3 BasisY;
    public Vector3 BasisZ;
}