using ModLoader.map.generator;

namespace ModLoader.handler;

public interface IMapManager {
    public void RegisterGenerator<T>(ulong worldId) where T : IWorldGenerator;
}