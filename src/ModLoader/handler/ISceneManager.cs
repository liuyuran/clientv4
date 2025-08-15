using Godot;

namespace ModLoader.handler;

public interface ISceneManager {
    public void OpenSceneModal(string mod, string path, Node node);
}