using Friflo.Engine.ECS;
using game.scripts.gui.InGameUI;
using game.scripts.renderer;
using Godot;

namespace game.scripts.utils;

public static class GameNodeReference {
    public static Node CurrentScene;
    public static PackedScene StartScenePacked;
    public static PackedScene GamingScenePacked;
    public static InGamingUI UI;
    public static EntityStore World;
    public static WorldContainer WorldContainer;
}