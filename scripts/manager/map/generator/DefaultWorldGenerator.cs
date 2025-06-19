using game.scripts.config;
using game.scripts.manager.blocks;
using game.scripts.renderer;
using game.scripts.utils;
using Godot;
using JetBrains.Annotations;

namespace game.scripts.manager.map.generator;

[UsedImplicitly]
public class DefaultWorldGenerator: IWorldGenerator {
    private long _seed;
    
    public void SetSeed(long seed) {
        _seed = seed;
    }

    public BlockData[][][] GenerateTerrain(Vector3I position) {
        var dirtId = BlockManager.instance.GetBlockId<Dirt>();
        var waterId = BlockManager.instance.GetBlockId<Water>();
        var data =  new BlockData[Config.ChunkSize][][];
        switch (position.Y) {
            case > 0: {
                for (var x = 0; x < Config.ChunkSize; x++) {
                    data[x] = new BlockData[Config.ChunkSize][];
                    for (var y = 0; y < Config.ChunkSize; y++) {
                        data[x][y] = new BlockData[Config.ChunkSize];
                        for (var z = 0; z < Config.ChunkSize; z++) {
                            data[x][y][z] = new BlockData {
                                BlockId = 0,
                                Direction = Direction.None
                            };
                        }
                    }
                }

                break;
            }
            case 0: {
                for (var x = 0; x < Config.ChunkSize; x++) {
                    data[x] = new BlockData[Config.ChunkSize][];
                    for (var y = 0; y < Config.ChunkSize; y++) {
                        data[x][y] = new BlockData[Config.ChunkSize];
                        for (var z = 0; z < Config.ChunkSize; z++) {
                            data[x][y][z] = new BlockData {
                                BlockId = y <= 0 ? waterId : 0,
                                Direction = Direction.None
                            };
                        }
                    }
                }

                break;
            }
            default: {
                for (var x = 0; x < Config.ChunkSize; x++) {
                    data[x] = new BlockData[Config.ChunkSize][];
                    for (var y = 0; y < Config.ChunkSize; y++) {
                        data[x][y] = new BlockData[Config.ChunkSize];
                        for (var z = 0; z < Config.ChunkSize; z++) {
                            data[x][y][z] = new BlockData {
                                BlockId = dirtId,
                                Direction = Direction.None
                            };
                        }
                    }
                }

                break;
            }
        }

        return data;
    }
}