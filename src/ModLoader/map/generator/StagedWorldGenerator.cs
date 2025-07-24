using DotnetNoise;
using ModLoader.config;
using ModLoader.map.stage;
using ModLoader.map.util;
using ModLoader.util;

namespace ModLoader.map.generator;

public abstract class StagedWorldGenerator: IWorldGenerator {
    private FastNoise? _noise;
    private readonly List<ITerrainGenerateStage> _stages = [];
    
    public void SetSeed(long seed) {
        _noise = new FastNoise((int)seed);
    }

    public abstract string GetName();

    protected void AddStage(ITerrainGenerateStage stage) {
        _stages.Add(stage);
    }
    
    public BlockData[][][] GenerateTerrain(Vector3I chunkPosition) {
        if (_noise == null) throw new InvalidOperationException("Noise generator is not initialized. Call SetSeed first.");
        var data = new TerrainDataCache {
            Position = chunkPosition,
            HeightMap = new int[Config.ChunkSize][],
            BlockData = new BlockData[Config.ChunkSize][][],
            Noise = _noise
        };

        for (var x = 0; x < Config.ChunkSize; x++) {
            data.HeightMap[x] = new int[Config.ChunkSize];
            data.BlockData[x] = new BlockData[Config.ChunkSize][];
            for (var z = 0; z < Config.ChunkSize; z++) {
                data.BlockData[x][z] = new BlockData[Config.ChunkSize];
            }
        }

        foreach (var stage in _stages) {
            stage.GenerateTerrain(data);
        }

        return data.BlockData;
    }
}