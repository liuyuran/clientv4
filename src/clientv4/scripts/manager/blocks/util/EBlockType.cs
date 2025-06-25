namespace game.scripts.manager.blocks.util;

public enum EBlockType {
    Solid, // Solid blocks that cannot be passed through
    Liquid, // Liquid blocks that can be passed through but may have special properties
    Gas, // Gas blocks that can be passed through and may have special properties
}