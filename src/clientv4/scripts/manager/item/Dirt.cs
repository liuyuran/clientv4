using game.scripts.manager.blocks;
using game.scripts.manager.item.composition;
using Block = game.scripts.manager.item.composition.Block;

namespace game.scripts.manager.item;

/// <summary>
/// example item for the dirt block.
/// it should be used in the game as a dirt item, and place a dirt block when used it.
/// </summary>
public class Dirt: Item {
    public override string name => "";
    public override string iconPath => "core:/texture/item/dirt.png";

    public Dirt() {
        this.SetBlock(new Block.BlockConfig {
            BlockId = BlockManager.instance.GetBlockId(new blocks.Dirt().name)
        });
    }
}