using ModLoader.block;
using ModLoader.block.util;

namespace Core.block;

public class Water: Block {
    public override string name => "water";
    public override string texturePath => "core:/texture/block/sample.png";
    public override EBlockType blockType => EBlockType.Liquid;
    public override bool transparent => true;
}