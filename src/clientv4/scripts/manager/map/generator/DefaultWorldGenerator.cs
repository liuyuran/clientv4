using game.scripts.manager.map.stage;
using Godot;

namespace game.scripts.manager.map.generator;

public class DefaultWorldGenerator : StagedWorldGenerator {
    public DefaultWorldGenerator() {
        AddStage(new FlatGroundBaseStage());
    }

    public override string GetName() {
        return TranslationServer.Translate("flat_land", "world_type");
    }
}