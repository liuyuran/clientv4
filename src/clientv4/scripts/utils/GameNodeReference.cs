using game.scripts.gui.InGameUI;
using Godot;

namespace game.scripts.utils;

public static class GameNodeReference {
    public static Node CurrentScene;
    public static PackedScene StartScenePacked;
    public static PackedScene GamingScenePacked;
    public static InGamingUI UI;
}