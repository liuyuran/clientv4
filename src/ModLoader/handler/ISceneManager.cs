using ModLoader.scene;

namespace ModLoader.handler;

public interface ISceneManager {
    public void OpenSceneModal(string mod, string path, NodeController controller);
}