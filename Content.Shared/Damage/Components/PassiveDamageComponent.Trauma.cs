using Content.Medical.Common.Damage;

namespace Content.Shared.Damage.Components;

public sealed partial class PassiveDamageComponent
{
    /// <summary>
    /// How passive damage split damage between parts
    /// Split for damage and SplitEnsureAllDamagedAndOrganic for passive regen
    /// MOCHO, I DON'T CARE -> COME AND FIX YOUR MED!!
    /// </summary>
    [DataField]
    public SplitDamageBehavior SplitBehavior = SplitDamageBehavior.Split;
}
