using ModLoader.map.util;
using ModLoader.util;

namespace ModLoader.map.generator;

public interface IWorldGenerator {
    void SetSeed(long seed);

    string GetName();
    
    /// <summary>
    /// generate terrain for the specified chunk position in the world.
    /// will use registered stages to generate terrain.
    /// </summary>
    BlockData[][][] GenerateTerrain(Vector3I chunkPosition);
}