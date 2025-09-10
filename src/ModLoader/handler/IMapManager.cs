using ModLoader.map.generator;

namespace ModLoader.handler;

public interface IMapManager {
    public void RegisterGenerator<T>(int worldId) where T : IWorldGenerator;
}