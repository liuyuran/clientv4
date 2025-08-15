using Godot;
using ModLoader.util;

namespace game.scripts.server.ECSBridge.block;

public struct SignalBlockChanged {
    public ulong BlockId;
    public Direction Direction;
    public ulong WorldId;
    public Vector3 Position;
}