using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._DV.CosmicCult.Components;

[RegisterComponent, Access(typeof(MalignRiftSpawnRule))]
public sealed partial class MalignRiftSpawnRuleComponent : Component
{
    [DataField] public EntProtoId MalignRift = "CosmicMalignRift";
    [DataField] public SoundSpecifier Tier2Sound = new SoundPathSpecifier("/Audio/_DV/CosmicCult/tier1.ogg");
}
