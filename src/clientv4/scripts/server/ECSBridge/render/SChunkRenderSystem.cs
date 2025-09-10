using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using game.scripts.utils;
using Godot;

namespace game.scripts.server.ECSBridge.render;

public class SChunkRenderSystem(EntityStore world, Node3D sceneRoot) : QuerySystem<CNodeLink, CGridIndex> {
    private readonly ConcurrentDictionary<Vector4I, CNodeLink> _chunkCache = new(); // cache chunk nodes via its location
    private readonly ConcurrentBag<Vector4I> _processing = []; // processing chunk render thread
    private readonly ComponentIndex<CGridIndex, Vector4I> _index = world.ComponentIndex<CGridIndex, Vector4I>(); 

    protected override void OnUpdate() {
        if (!_processing.IsEmpty) return;
        var commandBuffer = world.GetCommandBuffer().Synced;
        var chunkNeedUpdate = new ConcurrentDictionary<Vector4I, List<Entity>>();
        Query.ForEachEntity((ref CNodeLink link, ref CGridIndex grid, Entity entity) => {
            if (!link.Dirty) return;
            if (!chunkNeedUpdate.TryGetValue(grid.GetIndexedValue(), out var entities)) {
                entities = [];
                chunkNeedUpdate.TryAdd(grid.GetIndexedValue(), entities);
            }
            entities.Add(entity);
            link.Dirty = false;
        });
        var chunkNeedUpdateKeys = chunkNeedUpdate.Keys.ToImmutableArray();
        var groupTask = WorkerThreadPool.AddGroupTask(Callable.From<int>(index => {
            _processing.Add(chunkNeedUpdateKeys[index]);
            var unit = chunkNeedUpdate[chunkNeedUpdateKeys[index]];
            UpdateGridNode(chunkNeedUpdateKeys[index], unit, ref commandBuffer);
            _processing.TryTake(out _);
        }), chunkNeedUpdateKeys.Length);
        WorkerThreadPool.WaitForGroupTaskCompletion(groupTask);
        commandBuffer.Playback();
    }
    
    private void UpdateGridNode(Vector4I position, List<Entity> entities, ref CommandBufferSynced commandBufferSynced) {
        var shouldNotUpdateRender = PlatformUtil.isDedicatedServer;
        if (!shouldNotUpdateRender) UpdateChunkCube(position, entities);
        UpdateChunkCollider(position, entities);
        foreach (var entity in entities) {
            commandBufferSynced.AddComponent(entity.Id, _chunkCache[position]);
        }
    }
    
    private void UpdateChunkCube(Vector4I position, List<Entity> entities) {
        if (!_chunkCache.TryGetValue(position, out var link)) {
            // TODO create chunk node and add link component for entity, then add cache
        }
    }
    
    private void UpdateChunkCollider(Vector4I position, List<Entity> entities) {
        //
    }
}