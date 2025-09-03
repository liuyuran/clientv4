using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using game.scripts.manager.blocks;
using game.scripts.manager.loot;
using game.scripts.manager.map;
using game.scripts.manager.player;
using game.scripts.server.ECSBridge.input;
using game.scripts.server.ECSBridge.render;
using game.scripts.server.ECSBridge.sync;
using Godot;
using ModLoader.block.util;
using ModLoader.util;
using Vector3I = Godot.Vector3I;

namespace game.scripts.server.ECSBridge.block;

public class SBlockDestroyOrPlace(EntityStore world) : QuerySystem<CPeer, CPhysicsVelocity, CCamera, CInputEvent> {
    private ulong _lastActive;
    private const ulong ActiveCooldown = 300;
    private const ulong RayRange = 5;
    
    protected override void OnUpdate() {
        Query.ForEachEntity((ref CPeer peer, ref CPhysicsVelocity velocity, ref CCamera camera, ref CInputEvent inputEvent, Entity entity) => {
            if (inputEvent.Digging) {
                // check raycast, which face clicked
                var ray = camera.Camera.ProjectRayOrigin(inputEvent.MouseClickPosition);
                var rayDirection = camera.Camera.ProjectRayNormal(inputEvent.MouseClickPosition);
                var spaceState = camera.SpaceState;
                var result = spaceState.IntersectRay(new PhysicsRayQueryParameters3D {
                    CollideWithAreas = true,
                    CollideWithBodies = true,
                    From = ray,
                    To = ray + rayDirection * RayRange,
                    Exclude = [velocity.Rid],
                    HitBackFaces = true,
                    HitFromInside = false
                });
                if (result.Count > 0) {
                    var hitPosition = (Vector3)result["position"];
                    var hitNormal = (Vector3)result["normal"];
                    // is collider a StaticBody3D? if not, we can ignore it
                    // if (result["collider"].GetType() != typeof(StaticBody3D)) return;
                    // var collider = result["collider"].As<StaticBody3D>().GetParent();
                    var target = new Vector3I();
                    var targetF = hitPosition - hitNormal * 0.01f;
                    target.X = Mathf.FloorToInt(targetF.X);
                    target.Y = Mathf.FloorToInt(targetF.Y);
                    target.Z = Mathf.FloorToInt(targetF.Z);
                    // Here you can add logic to handle the block interaction, like breaking or placing blocks
                    var blockId = MapManager.instance.GetBlockIdByPosition(target);
                    if (blockId != 0 && _lastActive + ActiveCooldown < Time.GetTicksMsec()) {
                        // TODO add block breaking animation and sound and place drop item
                        var player = PlayerManager.instance.GetPlayerByPeerId(peer.PeerId);
                        GenerateDropItem(target, player.playerId);
                        entity.EmitSignal(new SignalBlockChanged {
                            Position = target,
                            BlockId = 0,
                            Direction = Direction.None,
                            WorldId = 0
                        });
                        _lastActive = Time.GetTicksMsec();
                    }
                }
            }
            if (inputEvent.Placing) {
                // check raycast, which face clicked
                var ray = camera.Camera.ProjectRayOrigin(inputEvent.MouseClickPosition);
                var rayDirection = camera.Camera.ProjectRayNormal(inputEvent.MouseClickPosition);
                var spaceState = camera.SpaceState;
                var result = spaceState.IntersectRay(new PhysicsRayQueryParameters3D {
                    CollideWithAreas = true,
                    CollideWithBodies = true,
                    From = ray,
                    To = ray + rayDirection * RayRange,
                    Exclude = [velocity.Rid],
                    HitBackFaces = true,
                    HitFromInside = false
                });
                if (result.Count > 0) {
                    var hitPosition = (Vector3)result["position"];
                    var hitNormal = (Vector3)result["normal"];
                    // is collider a StaticBody3D?
                    // if not, we can ignore it
                    var collider = result["collider"].As<StaticBody3D>().GetParent();
                    var target = new Vector3I();
                    var targetF = hitPosition + hitNormal * 0.01f;
                    target.X = Mathf.FloorToInt(targetF.X);
                    target.Y = Mathf.FloorToInt(targetF.Y);
                    target.Z = Mathf.FloorToInt(targetF.Z);
                    // Here you can add logic to handle the block interaction, like breaking or placing blocks
                    var blockId = MapManager.instance.GetBlockIdByPosition(target);
                    if (blockId != null && (blockId == 0 || BlockManager.instance.GetBlock(blockId.Value).blockType != EBlockType.Solid) && _lastActive + ActiveCooldown < Time.GetTicksMsec()) {
                        entity.EmitSignal(new SignalBlockChanged {
                            Position = target,
                            BlockId = 1, // TODO use dynamic block id
                            Direction = Direction.None,
                            WorldId = 0
                        });
                        _lastActive = Time.GetTicksMsec();
                    }
                }
            }
        });
    }
    
    private void GenerateDropItem(Vector3I position, ulong playerId) {
        var blockId = MapManager.instance.GetBlockIdByPosition(position);
        if (blockId == null) return;
        var buffer = CommandBuffer;
        var entity = buffer.CreateEntity();
        buffer.AddComponent(entity, new CRenderType {
            Type = ERenderType.DropItemPack
        });
        buffer.AddComponent(entity, new CTransform {
            Position = position
        });
        buffer.AddComponent(entity, new CPhysicsVelocity());
        buffer.AddComponent(entity, new CDropItem {
            BlockId = blockId.Value,
            PlayerId = playerId
        });
    }
}