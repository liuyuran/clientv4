using System;
using game.scripts.server;
using Godot;

namespace game.scripts.utils;

public static class PlatformUtil {
    public static bool goDotMode { get; set; } = false;
    
    public static bool isNetworkMaster => ServerStartupConfig.instance.isLocalServer || isDedicatedServer;
    public static bool isDedicatedServer => OS.HasFeature("dedicated_server");
    
    public static ulong GetTimestamp() {
        // 获取当前时间的时间戳
        if (goDotMode) {
            return Time.GetTicksMsec();
        }

        return (ulong)new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();;
    }
}