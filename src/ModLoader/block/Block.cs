using ModLoader.block.util;

namespace ModLoader.block;

public abstract class Block {
    public virtual string name => throw new System.NotImplementedException();
    public virtual string texturePath => throw new System.NotImplementedException();
    public virtual EBlockType blockType => EBlockType.Solid;
    public virtual bool transparent => false;
}