using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using game.scripts.server.ECSBridge.input;
using game.scripts.server.ECSBridge.sync;
using Godot;

namespace game.scripts.server.ECSBridge.gravity;

public class SJump(EntityStore world) : QuerySystem<CInputEvent, CPhysicsStatus, CPhysicsVelocity> {
    private ulong _lastActive;
    private const ulong ActiveCooldown = 300;
    
    protected override void OnUpdate() {
        var commandBuffer = world.GetCommandBuffer();
        Query.ForEachEntity(
            (ref CInputEvent inputEvent, ref CPhysicsStatus physicsStatus, ref CPhysicsVelocity physicsVelocity, Entity entity) => {
                if (inputEvent.Jump && !physicsStatus.Jumping && _lastActive + ActiveCooldown < Time.GetTicksMsec()) {
                    physicsVelocity.Velocity.Y = 5000; // Adjust the jump force as needed
                    commandBuffer.AddTag<THasDirtData>(entity.Id);
                    _lastActive = Time.GetTicksMsec();
                }
            }
        );
        commandBuffer.Playback();
    }
}