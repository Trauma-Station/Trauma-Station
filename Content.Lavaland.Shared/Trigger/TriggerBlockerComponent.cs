using Content.Shared.Whitelist;

namespace Content.Lavaland.Shared.Trigger;

/// <summary>
/// Prevents triggering depending on map.
/// </summary>
[RegisterComponent]
public sealed partial class TriggerBlockerComponent : Component
{
    [DataField]
    public EntityWhitelist? MapWhitelist;

    [DataField]
    public EntityWhitelist? MapBlacklist;
}
