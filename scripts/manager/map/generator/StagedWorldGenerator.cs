using System.Collections.Generic;
using game.scripts.config;
using game.scripts.manager.map.stage;
using game.scripts.manager.map.util;
using game.scripts.renderer;
using Godot;

namespace game.scripts.manager.map.generator;

public abstract class StagedWorldGenerator: IWorldGenerator {
    private readonly FastNoiseLite _noise = new();
    private readonly List<ITerrainGenerateStage> _stages = [];
    
    public void SetSeed(long seed) {
        _noise.Seed = (int)seed;
    }
    
    protected void AddStage(ITerrainGenerateStage stage) {
        _stages.Add(stage);
    }
    
    public BlockData[][][] GenerateTerrain(Vector3I chunkPosition) {
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