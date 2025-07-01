using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using game.scripts.server.ECSBridge.input;
using game.scripts.server.ECSBridge.sync;
using Godot;

namespace game.scripts.server.ECSBridge.gravity;

public class SJumpAndGravity(EntityStore world) : QuerySystem<CInputEvent, CPhysicsStatus, CPhysicsVelocity, CJumpStatus> {
    private const ulong ActiveCooldown = 300;
    
    protected override void OnUpdate() {
        var commandBuffer = world.GetCommandBuffer();
        // apply jump
        Query.ForEachEntity(
            (ref CInputEvent inputEvent, ref CPhysicsStatus physicsStatus, ref CPhysicsVelocity physicsVelocity, ref CJumpStatus jumpStatus, Entity entity) => {
                if (inputEvent.Jump && jumpStatus.LastAction + ActiveCooldown < Time.GetTicksMsec()) {
                    jumpStatus.LastAction = Time.GetTicksMsec();
                    if (!physicsStatus.Jumping) {
                        jumpStatus.JumpStartTime = Time.GetTicksMsec();
                    }
                }
                if (Time.GetTicksMsec() - jumpStatus.JumpStartTime > 300) return; // if jump started more than 500 ms ago, do nothing
                physicsVelocity.Velocity.Y += (Time.GetTicksMsec() - jumpStatus.JumpStartTime) switch {
                    < 100 => 1400,
                    < 200 => 600,
                    _ => 50
                };
                commandBuffer.AddTag<THasDirtData>(entity.Id);
            }
        );
        // apply gravity
        Query.ForEachEntity(
            (ref CInputEvent _, ref CPhysicsStatus _, ref CPhysicsVelocity physicsVelocity, ref CJumpStatus _, Entity _) => {
                physicsVelocity.Velocity.Y += -400;
            }
        );
        commandBuffer.Playback();
    }
}