using System.Collections.Generic;
using Friflo.Engine.ECS;
using game.scripts.manager.item;
using game.scripts.renderer;
using game.scripts.server.ECSBridge.input;
using game.scripts.server.ECSBridge.render;
using game.scripts.utils;
using Godot;

namespace game.scripts.server.ECSBridge.sync;

public static class ItemRenderUtil {
    public static void CreateItemEntity(Entity entity, PackedScene itemPrototype, Node root, Dictionary<Entity, Node3D> entityNodes) {
        var worldContainerNode = root.FindNodeByName<WorldContainer>("worlds");
        var worldNode = worldContainerNode.GetCurrentSubViewport();
        var node = ItemManager.instance.GetItemDropModel(0);
        node.Name = $"Item_{entity.Id}";
        worldNode.AddChild(node);
        entityNodes.Add(entity, node);
    }

    public static Entity CreateItemEntity(EntityStore world, ulong itemId, Vector3 position) {
        var entity = world.CreateEntity(new CRenderType {
            Type = ERenderType.DropItemPack
        }, new CTransform {
            Position = position
        }, new CPhysicsVelocity());
        return entity;
    }
}