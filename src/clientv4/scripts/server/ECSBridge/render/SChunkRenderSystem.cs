using System.Collections.Generic;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Godot;

namespace game.scripts.server.ECSBridge.render;

public class SChunkRenderSystem(EntityStore world, Node3D sceneRoot) : QuerySystem<CNodeLink, CBlockInfo, CGridIndex> {
    private readonly Dictionary<CGridIndex, CNodeLink> _chunkCache = new();
    // WorkerThreadPoolInstance
    // query.OnComponentAdded<CNodeLink>((entity, link) => InitializeGridNode(entity, link));
    // query.OnComponentChanged<CBlockInfo>((entity, block) => MarkGridDirty(entity));

    protected override void OnUpdate() {
        var commandBuffer = world.GetCommandBuffer().Synced;
        var queryJob = Query.ForEach((chunk, blockInfo, gridIndex, entities) => {
            // generate mesh and call defer it
            for (var n = 0; n < entities.Length; n++) {
                if (!chunk[n].Dirty) return;
                if ((chunk[n].ClientNode != null && chunk[n].ClientNode.GetParent() != sceneRoot) ||
                    (chunk[n].ServerNode != null && chunk[n].ServerNode.GetParent() != sceneRoot)) {
                    // if not attach to the scene root node, then attach it
                    InitializeGridNode(gridIndex[n], chunk[n]);
                }
                chunk[n].Dirty = false;                
            }
        });
        queryJob.RunParallel();
    }
    
    private void InitializeGridNode(CGridIndex entity, CNodeLink link) {
        /*var gridPos = entity.GetComponent<Grid>().gridPos;
        if (OS.GetName() == "Server") {
            link.physicsNode = new StaticBody3D();
            link.physicsNode.Position = gridPos * GridUtils.GRID_SIZE;
            link.physicsNode.CollisionLayer = 1;
            var shape = new CollisionShape3D { Shape = GenerateCollisionShape(entity) };
            link.physicsNode.AddChild(shape);
            sceneRoot.AddChild(link.physicsNode);
        } else {
            link.meshNode = new MeshInstance3D();
            link.meshNode.Position = gridPos * GridUtils.GRID_SIZE;
            threadPool.AddTask(() => {
                link.meshNode.Mesh = GenerateMesh(entity);
                CallDeferred("AddChildToScene", link.meshNode);
            });
        }
        link.dirty = false;*/
    }
    
    private void UpdateChunkCube(Entity entity, CNodeLink link) {
        //
    }
    
    private void UpdateChunkCollider(Entity entity, CNodeLink link) {
        //
    }
}