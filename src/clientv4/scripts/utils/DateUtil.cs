using System;
using Godot;

namespace game.scripts.utils;

public static class DateUtil {
    public static bool goDotMode { get; set; } = false;
    
    public static ulong GetTimestamp() {
        // 获取当前时间的时间戳
        if (goDotMode) {
            return Time.GetTicksMsec();
        }

        return (ulong)new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();;
    }
}