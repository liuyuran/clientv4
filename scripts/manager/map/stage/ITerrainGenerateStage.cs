using game.scripts.manager.map.util;
using Godot;

namespace game.scripts.manager.map.stage;

/// <summary>
/// generate stage for terrain.
/// </summary>
public interface ITerrainGenerateStage {
    void GenerateTerrain(TerrainDataCache data);
}