using Robust.Shared.Prototypes;

namespace Content.Shared._DV.CosmicCult.Components;

/// <summary>
/// Indicates that an entity can be transmuted into the given prototype by a cosmic cultist
/// </summary>
[RegisterComponent]
public sealed partial class CosmicTransmutableComponent : Component
{
    /// <summary>
    /// What will the object turn into. If null, also hides the examine text (for parenting shenanigans)
    /// </summary>
    [DataField(required: true)]
    public EntProtoId? TransmuteTo;

    [DataField]
    public bool RequiresEmpowerment;
}
