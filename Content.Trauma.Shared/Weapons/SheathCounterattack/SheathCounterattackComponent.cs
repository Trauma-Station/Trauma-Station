// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Weapons.SheathCounterattack;

/// <summary>
/// Added to sheath to allow it to counterattack with sheathed weapon
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SheathCounterattackComponent : Component
{
    [DataField]
    public TimeSpan CounterWindowTime = TimeSpan.FromSeconds(1);

    [DataField]
    public SoundSpecifier CounterAttackingSound = new SoundPathSpecifier("/Audio/_Goobstation/Effects/Parry/parry.ogg");

    [DataField]
    public string SlotId = "item";

    [DataField]
    public EntProtoId<CounterAttackingStatusEffectComponent> StatusEffect = "CounterAttackingStatusEffect";

    [DataField]
    public TimeSpan BlockEffectTime = TimeSpan.FromSeconds(15);

    [DataField]
    public SoundSpecifier CounterAttackSuccessSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/parry.ogg");

    [DataField]
    public bool CanCounterNpc;

    [DataField]
    public Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> ExtraWoundSeverityMultipliers = new();

    [DataField]
    public float ExtraArmorPenetration;

    [DataField]
    public float ExtraDamageMultiplier;
}
