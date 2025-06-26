using DotnetNoise;
using game.scripts.renderer;
using Godot;

namespace game.scripts.manager.map.util;

public class TerrainDataCache {
    public int[][] HeightMap;
    public Vector3I Position;
    public BlockData[][][] BlockData;
    public FastNoise Noise;
}