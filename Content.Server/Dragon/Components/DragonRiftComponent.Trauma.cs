using Robust.Shared.Prototypes;

namespace Content.Server.Dragon;

public sealed partial class DragonRiftComponent
{
    [DataField]
    public float StrongSpawnChance = 0.15f;

    [DataField("spawnStrong")]
    public EntProtoId SpawnPrototypeStrong = "MobSharkDragon";
}
