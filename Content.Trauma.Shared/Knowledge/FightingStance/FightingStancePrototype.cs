using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.FightingStance;

/// <summary>
/// Specifies a certain fighting stance.
/// </summary>
[Prototype]
public sealed partial class FightingStancePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public int AttackMod;

    [DataField]
    public int DefenseMod;

    [DataField]
    public int SpeedMod;

    [DataField]
    public int DamageMod;

    [DataField]
    public int DefenseDice;

    /// <summary>
    /// How many weapons do you need?
    /// </summary>
    [DataField]
    public int WeaponCount = 0;

    /// <summary>
    /// How many weapons need to be wielded?
    /// </summary>
    [DataField]
    public int WieldCount = 0;

    /// <summary>
    /// How many shields?
    /// </summary>
    [DataField]
    public int ShieldCount = 0;
}
