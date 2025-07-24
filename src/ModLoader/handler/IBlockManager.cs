using ModLoader.block;

namespace ModLoader.handler;

public interface IBlockManager {
    public void Register<T>() where T : Block, new();

    public ulong GetBlockId<T>() where T : Block;
}