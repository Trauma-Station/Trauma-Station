using Content.Shared.Whitelist;

namespace Content.DV.Shared.CosmicCult.Components;

[RegisterComponent]
public sealed partial class CosmicEffigyConditionComponent : Component
{
    [DataField]
    public EntityUid? EffigyTarget;

    [DataField]
    public EntityWhitelist? Blacklist;
}
