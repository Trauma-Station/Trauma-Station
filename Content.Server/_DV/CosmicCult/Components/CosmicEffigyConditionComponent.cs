using Content.Shared.Whitelist;

namespace Content.Server.Objectives.Components;

[RegisterComponent]
public sealed partial class CosmicEffigyConditionComponent : Component
{
    [DataField]
    public EntityUid? EffigyTarget;

    [DataField]
    public EntityWhitelist? Blacklist;
}
