using game.scripts.config;
using game.scripts.manager.blocks;
using game.scripts.manager.map.stage;
using game.scripts.manager.map.util;
using game.scripts.renderer;
using game.scripts.utils;

namespace Core.terrain.stage;

/// <summary>
/// all chunks of the world are random base on noise, this stage is used to generate normal ground terrain.
/// </summary>
public class NoiseGroundBaseStage : ITerrainGenerateStage {
    public void GenerateTerrain(TerrainDataCache data) {
        data.HeightMap = new int[Config.ChunkSize][];
        data.BlockData = new BlockData[Config.ChunkSize][][];

        for (var x = 0; x < Config.ChunkSize; x++) {
            data.HeightMap[x] = new int[Config.ChunkSize];
            for (var z = 0; z < Config.ChunkSize; z++) {
                switch (data.Position.Y) {
                    case > 0:
                        data.HeightMap[x][z] = 0;
                        break;
                    case 0:
                        var globalX = x + data.Position.X * Config.ChunkSize;
                        var globalZ = z + data.Position.Z * Config.ChunkSize;
                        data.HeightMap[x][z] = (int)((data.Noise.GetNoise(globalX, globalZ) + 1) / 2 * Config.ChunkSize);
                        break;
                    default:
                        data.HeightMap[x][z] = Config.ChunkSize - 1;
                        break;
                }
            }
        }
        var stoneId = BlockManager.instance.GetBlockId<Stone>();
        for (var x = 0; x < Config.ChunkSize; x++) {
            data.BlockData[x] = new BlockData[Config.ChunkSize][];
            for (var y = 0; y < Config.ChunkSize; y++) {
                data.BlockData[x][y] = new BlockData[Config.ChunkSize];
                for (var z = 0; z < Config.ChunkSize; z++) {
                    var maxHeight = data.HeightMap[x][z];
                    data.BlockData[x][y][z] = new BlockData {
                        BlockId = maxHeight >= y + data.Position.Y * Config.ChunkSize ? stoneId : 0,
                        Direction = Direction.None
                    };
                }
            }
        }
    }
}