using Godot;
using ModLoader.config;
using Vector3I = Godot.Vector3I;

namespace game.scripts.utils;

public static class Vector3ChunkUtil {
    // ModLoader.util.Vector3I 和 GoDot.Vector3I 的隐式双向转换
    public static Vector3I ToChunkPosition(this Vector3 position) {
        return new Vector3I(
            (int)Mathf.Floor(position.X / Config.ChunkSize),
            (int)Mathf.Floor(position.Y / Config.ChunkSize),
            (int)Mathf.Floor(position.Z / Config.ChunkSize)
        );
    }
    
    public static Vector3I ToLocalPosition(this Vector3 position) {
        var localPosition = new Vector3I(
            (int)(position.X % Config.ChunkSize),
            (int)(position.Y % Config.ChunkSize),
            (int)(position.Z % Config.ChunkSize)
        );
        if (localPosition.X < 0) localPosition.X += Config.ChunkSize;
        if (localPosition.Y < 0) localPosition.Y += Config.ChunkSize;
        if (localPosition.Z < 0) localPosition.Z += Config.ChunkSize;
        return localPosition;
    }
}