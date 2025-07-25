using Godot;

namespace game.scripts.utils;

public static class NodeUtil {
    public static T FindNodeByName<T>(this Node parent, string name) where T : Node {
        if (parent == null) return null;
        foreach (var child in parent.GetChildren()) {
            if (child is T node && node.Name == name) {
                return node;
            }
            var found = FindNodeByName<T>(child, name);
            if (found != null) return found;
        }
        return null;
    }
}