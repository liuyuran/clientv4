using Core.terrain.stage;
using ModLoader;
using ModLoader.map.generator;

namespace Core.terrain.generator;

public class DefaultWorldGenerator : StagedWorldGenerator {
    public DefaultWorldGenerator() {
        AddStage(new FlatGroundBaseStage());
    }

    public override string GetName() {
        return I18N.Tr("mod.core", "world_type.flat_land");
    }
}