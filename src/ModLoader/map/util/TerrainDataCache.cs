using DotnetNoise;
using ModLoader.util;

namespace ModLoader.map.util;

public class TerrainDataCache {
    public required int[][] HeightMap;
    public Vector3I Position;
    public required BlockData[][][] BlockData;
    public required FastNoise Noise;
}