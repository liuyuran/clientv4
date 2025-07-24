using ModLoader.handler;

namespace ModLoader;

public interface IMod {
    /// <summary>
    /// Called when the mod is loaded.
    /// </summary>
    void OnLoad(IModHandler handler);

    /// <summary>
    /// Called when the mod is unloaded.
    /// </summary>
    void OnUnload();

    /// <summary>
    /// Called when the game starts.
    /// </summary>
    void OnGameStart();

    /// <summary>
    /// Called when the game stops.
    /// </summary>
    void OnGameStop();
}