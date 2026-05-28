// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Talents.Components;

namespace Content.Trauma.Shared.Knowledge.Attribute.Attribute.Systems;

/// <summary>
/// This class handles all the talent relay events
/// </summary>
public sealed partial class TalentRelaySystem : EntitySystem
{
    private static readonly HashSet<ProtoId<DamageTypePrototype>> DamageTypes = new()
    {
        "Blunt",
        "Slash",
        "Piercing",
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DodgeComponent, GetDodgeSavingThrowEvent>(OnCalculateDodge);
        SubscribeLocalEvent<DamageTalentComponent, GetDamageModifierEvent>(OnCalculateDamage);
        SubscribeLocalEvent<DamageTalentComponent, GetSpeedModifierEvent>(OnCalculateSpeed);
        SubscribeLocalEvent<DamageTalentComponent, BeforeDamageChangedEvent>(OnCalculateHeal);
        SubscribeLocalEvent<ToughHideComponent, BeforeDamageChangedEvent>(OnCalculateResist);
    }

    private void OnCalculateDodge(Entity<DodgeComponent> ent, ref GetDodgeSavingThrowEvent args)
    {
        if (!TryComp<TalentComponent>(ent, out var talent))
            return;

        args.Mod += talent.Level;
    }

    private void OnCalculateDamage(Entity<DamageTalentComponent> ent, ref GetDamageModifierEvent args)
    {
        if (!TryComp<TalentComponent>(ent, out var talent))
            return;

        args.Mod += talent.Level;
    }

    private void OnCalculateSpeed(Entity<DamageTalentComponent> ent, ref GetSpeedModifierEvent args)
    {
        if (!TryComp<TalentComponent>(ent, out var talent))
            return;

        args.Mod += talent.Level;
    }

    private void OnCalculateHeal(Entity<DamageTalentComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (!TryComp<TalentComponent>(ent, out var talent))
            return;

        var heal = DamageSpecifier.GetNegative(args.Damage) * (talent.Level + 1);
        args.Damage.ExclusiveAdd(heal);
    }

    private void OnCalculateResist(Entity<ToughHideComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (!TryComp<TalentComponent>(ent, out var talent))
            return;

        var damageReduction = DamageSpecifier.GetPositive(args.Damage);
        foreach (var (key, amount) in damageReduction.DamageDict)
        {
            if (!DamageTypes.Contains(key))
                continue;

            damageReduction.DamageDict[key] = -FixedPoint2.Min(amount, FixedPoint2.New(talent.Level));
        }
        args.Damage.ExclusiveAdd(damageReduction);
    }
}
