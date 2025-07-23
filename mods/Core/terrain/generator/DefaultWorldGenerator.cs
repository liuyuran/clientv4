using Core.terrain.stage;
using game.scripts.manager.map.generator;
using Godot;
using ModLoader;

namespace Core.terrain.generator;

public class DefaultWorldGenerator : StagedWorldGenerator {
    public DefaultWorldGenerator() {
        AddStage(new FlatGroundBaseStage());
    }

    public override string GetName() {
        return I18N.Tr("world_type.flat_land", "mod.core");
    }
}