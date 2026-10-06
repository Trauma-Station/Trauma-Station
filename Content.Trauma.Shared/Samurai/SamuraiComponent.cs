

namespace Content.Trauma.Shared.Samurai;

/// <summary>
/// Component placed on a mob to make it a space ninja, able to use suit and glove powers.
/// Contains ids of all ninja equipment.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SamuraiComponent : Component
{
    /// <summary>
    /// Currently worn suit
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Suit;

    /// <summary>
    /// Bound energy polearm, set once equipped and never removed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? EnergyPolearm;

    /// <summary>
    /// Objective to complete after calling in a threat.
    /// </summary>
    [DataField]
    public EntProtoId TerrorObjective = "TerrorObjective";

    /// <summary>
    /// Objective to complete after setting everyone to arrest.
    /// </summary>
    [DataField]
    public EntProtoId MassArrestObjective = "MassArrestObjective";

    /// <summary>
    /// Objective to complete after the spider charge detonates.
    /// </summary>
    [DataField]
    public EntProtoId SpiderChargeObjective = "SpiderChargeObjective";
}
