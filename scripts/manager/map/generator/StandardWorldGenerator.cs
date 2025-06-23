using game.scripts.manager.map.stage;

namespace game.scripts.manager.map.generator;

public class StandardWorldGenerator: StagedWorldGenerator {
    public StandardWorldGenerator() {
        AddStage(new NoiseGroundBaseStage());
    }
}