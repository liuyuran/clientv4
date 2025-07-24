using Core.terrain.stage;
using ModLoader;
using ModLoader.map.generator;

namespace Core.terrain.generator;

public class StandardWorldGenerator: StagedWorldGenerator {
    public StandardWorldGenerator() {
        AddStage(new NoiseGroundBaseStage());
    }

    public override string GetName() {
        return I18N.Tr("mod.core", "world_type.standard_land");
    }
}