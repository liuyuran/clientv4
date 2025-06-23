using game.scripts.manager.map.stage;

namespace game.scripts.manager.map.generator;

public class DefaultWorldGenerator: StagedWorldGenerator {
    public DefaultWorldGenerator() {
        AddStage(new FlatGroundBaseStage());
    }
}