using Friflo.Engine.ECS;
using Godot;

namespace game.scripts.server.ECSBridge.input;

public struct CTransform : IComponent {
    /// <summary>
    /// position in world space
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// rotation in euler angles
    /// </summary>
    public Vector3 Rotation;

    /// <summary>
    /// x-forward that that object faces towards
    /// </summary>
    public Vector3 BasisX;
    
    /// <summary>
    /// y-forward that that object faces towards
    /// </summary>
    public Vector3 BasisY;
    
    /// <summary>
    /// z-forward that that object faces towards
    /// </summary>
    public Vector3 BasisZ;
}