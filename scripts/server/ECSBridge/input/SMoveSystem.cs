using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using game.scripts.server.ECSBridge.render;
using game.scripts.server.ECSBridge.sync;
using Godot;

namespace game.scripts.server.ECSBridge.input;

/// <summary>
/// 玩家移动模块
/// </summary>
public class SMoveSystem(EntityStore world): QuerySystem<CRenderType, CTransform, CInputEvent, CPhysicsVelocity> {
    private const float MoveSpeed = 500f;

    protected override void OnUpdate() {
        var commandBuffer = world.GetCommandBuffer();
        Query.ForEachEntity((ref CRenderType _, ref CTransform transform, ref CInputEvent inputEvent, ref CPhysicsVelocity velocity, Entity entity) => {
            var physicsForward = Vector3.Zero;
            if (inputEvent.MoveBackward) {
                physicsForward += transform.BasisZ;
            }
            if (inputEvent.MoveForward) {
                physicsForward -= transform.BasisZ;
            }
            if (inputEvent.MoveRight) {
                physicsForward += transform.BasisX;
            }
            if (inputEvent.MoveLeft) {
                physicsForward -= transform.BasisX;
            }
            if (inputEvent.Jump) {
                physicsForward += Vector3.Up;
            }
            if (inputEvent.Crouch) {
                physicsForward += Vector3.Down;
            }
            physicsForward = physicsForward.Normalized() * MoveSpeed;
            if (physicsForward == velocity.Velocity && 
                inputEvent.ForwardVector == velocity.Rotation &&
                physicsForward == Vector3.Zero &&
                inputEvent.ForwardVector == Vector2.Zero) return;
            velocity.Velocity = physicsForward;
            velocity.Rotation = inputEvent.ForwardVector;
            commandBuffer.AddTag<THasDirtData>(entity.Id);
        });
        commandBuffer.Playback();
    }
}