using Friflo.Engine.ECS;
using Godot;

namespace game.scripts.server.ECSBridge.input;

/// <summary>
/// physics velocity component.
/// </summary>
public struct CPhysicsVelocity : IComponent {
    /// <summary>
    /// the rid of the physics body
    /// </summary>
    public Rid Rid;

    /// <summary>
    /// the movement vector of the physics body
    /// </summary>
    public Vector3 Velocity;

    /// <summary>
    /// the rotation vector of the physics body
    /// </summary>
    public Vector2 Rotation;
}