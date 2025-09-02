using Friflo.Engine.ECS;
using game.scripts.manager.blocks;
using game.scripts.manager.item;
using game.scripts.manager.loot;
using game.scripts.renderer;
using game.scripts.server.ECSBridge.block;
using game.scripts.utils;
using Godot;

namespace game.scripts.server.ECSBridge.sync;

public static class ItemRenderUtil {
    public static void CreateItemEntity(Entity entity, PackedScene itemPrototype, Node root) {
        var dropItemComponent = entity.GetComponent<CDropItem>();
        var blockName = BlockManager.instance.GetBlock(dropItemComponent.BlockId).name;
        var lootResult = LootManager.instance.GetLootItems(blockName, dropItemComponent.PlayerId);
        var worldContainerNode = root.FindNodeByName<WorldContainer>("worlds");
        var worldNode = worldContainerNode.GetCurrentSubViewport();
        foreach (var lootItem in lootResult) {
            GD.Print($"Generating drop item: {lootItem.ItemName} x{lootItem.amount}");
            var node = itemPrototype.Instantiate<Node>();
            // need check should show a cube model or pieces model
            var itemModal = ItemManager.instance.GetItemDropModel(lootItem);
            node.AddChild(itemModal);
            worldNode.AddChild(node);
        }
    }
}