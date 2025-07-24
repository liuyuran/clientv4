namespace ModLoader.util;

public struct Vector3I(int x, int y, int z) {
    public int x { get; set; } = x;
    public int y { get; set; } = y;
    public int z { get; set; } = z;

    public override string ToString() {
        return $"({x}, {y}, {z})";
    }

    public static Vector3I zero => new(0, 0, 0);
}