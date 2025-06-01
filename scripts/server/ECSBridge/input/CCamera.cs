using Friflo.Engine.ECS;
using Godot;

namespace game.scripts.server.ECSBridge.input;

public struct CCamera: IComponent {
    public Camera3D Camera;
    public PhysicsDirectSpaceState3D SpaceState;
}