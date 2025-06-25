using Friflo.Engine.ECS;
using Godot;

namespace game.scripts.server.ECSBridge.input;

public struct CPhysicsVelocity: IComponent {
    public Rid Rid;
    public Vector3 Velocity;
    public Vector2 Rotation;
}