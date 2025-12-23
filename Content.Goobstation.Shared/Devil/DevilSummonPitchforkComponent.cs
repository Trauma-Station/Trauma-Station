using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Devil;

/// <summary>
/// Grants the Devil the ability to summon (or create) their pitchfork into their active hand. Very generic.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DevilSummonPitchforkComponent : Component
{
    [ViewVariables]
    public EntityUid? ActionEnt;

    [DataField]
    public EntProtoId ActionId = "ActionDevilSummonPitchfork";

    /// <summary>
    /// Prototype id of the item to summon.
    /// </summary>
    [DataField]
    public EntProtoId PitchforkPrototype = "DevilPitchfork";

    /// <summary>
    /// The current tracked machete entity.
    /// </summary>
    [ViewVariables]
    public EntityUid? PitchforkUid;
}
