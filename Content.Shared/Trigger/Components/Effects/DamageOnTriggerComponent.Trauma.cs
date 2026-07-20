using Content.Medical.Common.Targeting;
using Content.Shared.Whitelist;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Whitelist and blacklist support for the target, as well as limb targeting
/// </summary>
public sealed partial class DamageOnTriggerComponent
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// The body part to target damage with.
    /// </summary>
    [DataField]
    public TargetBodyPart? TargetPart;
}
