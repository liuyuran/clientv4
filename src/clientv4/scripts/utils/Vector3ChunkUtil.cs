using Godot;
using ModLoader.config;
using Vector3I = Godot.Vector3I;

namespace game.scripts.utils;

public static class Vector3ChunkUtil {
    public static string ToArchiveString(this Vector3 vector) {
        return $"{vector.X},{vector.Y},{vector.Z}";
    }

    public static bool TryParse(this Vector3 vector, in string from, out Vector3 result) {
        result = new Vector3();
        var parts = from.Split(',');
        if (parts.Length != 3) return false;
        
        if (float.TryParse(parts[0], out var x) &&
            float.TryParse(parts[1], out var y) &&
            float.TryParse(parts[2], out var z)) {
            result = new Vector3(x, y, z);
            return true;
        }
        return false;
    }
    
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