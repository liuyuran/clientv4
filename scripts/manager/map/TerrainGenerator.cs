using System;
using System.Collections.Generic;
using game.scripts.manager.map.generator;
using game.scripts.renderer;
using Godot;

namespace game.scripts.manager.map;

public class TerrainGenerator {
    private readonly long _seed;
    private readonly Dictionary<ulong, IWorldGenerator> _generators = new();

    public TerrainGenerator(long seed) {
        _seed = seed;
        RegistryGenerator<DefaultWorldGenerator>(0);
    }
    
    private void RegistryGenerator<T>(ulong worldId) where T: IWorldGenerator {
        var generator = (IWorldGenerator)Activator.CreateInstance(typeof(T));
        if (generator == null) {
            GD.PrintErr($"cannot create instance for {typeof(T).Name}");
            return;
        }
        generator.SetSeed(_seed);
        _generators[worldId] = generator;
        GD.Print($"注册生成器: {typeof(T).Name}");
    }

    public BlockData[][][] GenerateTerrain(ulong worldId, Vector3I chunkPosition) {
        if (!_generators.TryGetValue(worldId, out var generator)) {
            GD.PrintErr($"no available generate for world {worldId}");
            return null;
        }
        return generator.GenerateTerrain(chunkPosition);
    }
}