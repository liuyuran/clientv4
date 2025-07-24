using Core.terrain.stage;
using ModLoader;
using ModLoader.map.generator;

namespace Core.terrain.generator;

public class StandardWorldGenerator: StagedWorldGenerator {
    public StandardWorldGenerator() {
        AddStage(new NoiseGroundBaseStage());
    }

    public override string GetName() {
        return I18N.Tr("world_type.standard_land", "mod.core");
    }
}