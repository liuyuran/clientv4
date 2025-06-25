using game.scripts.manager.map.stage;
using Godot;

namespace game.scripts.manager.map.generator;

public class StandardWorldGenerator: StagedWorldGenerator {
    public StandardWorldGenerator() {
        AddStage(new NoiseGroundBaseStage());
    }

    public override string GetName() {
        return TranslationServer.Translate("standard_land", "world_type");
    }
}