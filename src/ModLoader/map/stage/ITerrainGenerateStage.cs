using ModLoader.map.util;

namespace ModLoader.map.stage;

/// <summary>
/// generate stage for terrain.
/// </summary>
public interface ITerrainGenerateStage {
    void GenerateTerrain(TerrainDataCache data);
}