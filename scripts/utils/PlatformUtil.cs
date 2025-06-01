using game.scripts.server;
using Godot;

namespace game.scripts.utils;

public static class PlatformUtil {
    public static bool isNetworkMaster => ServerStartupConfig.instance.isLocalServer || isDedicatedServer;
    public static bool isDedicatedServer => OS.HasFeature("dedicated_server");
}