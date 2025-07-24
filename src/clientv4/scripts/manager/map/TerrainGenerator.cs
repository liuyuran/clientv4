using System;
using System.Collections.Generic;
using game.scripts.renderer;
using Godot;
using ModLoader.map.generator;
using ModLoader.map.util;

namespace game.scripts.manager.map;

public class TerrainGenerator(long seed) {
    private readonly Dictionary<ulong, IWorldGenerator> _generators = new();

    public void RegistryGenerator<T>(ulong worldId) where T: IWorldGenerator {
        var generator = (IWorldGenerator)Activator.CreateInstance(typeof(T));
        if (generator == null) {
            GD.PrintErr($"cannot create instance for {typeof(T).Name}");
            return;
        }
        generator.SetSeed(seed);
        _generators[worldId] = generator;
    }

    public BlockData[][][] GenerateTerrain(ulong worldId, Vector3I chunkPosition) {
        if (_generators.TryGetValue(worldId, out var generator)) 
            return generator.GenerateTerrain(new ModLoader.util.Vector3I(chunkPosition.X, chunkPosition.Y, chunkPosition.Z));
        GD.PrintErr($"no available generate for world {worldId}");
        return null;
    }
}