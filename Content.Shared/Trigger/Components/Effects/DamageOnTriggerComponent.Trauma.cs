using Content.Shared.Whitelist;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Whitelist and blacklist support for the target
/// </summary>
public sealed partial class DamageOnTriggerComponent
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
}
