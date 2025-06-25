using game.scripts.config;
using Godot;

namespace game.scripts.utils;

public static class Vector3ChunkUtil {
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