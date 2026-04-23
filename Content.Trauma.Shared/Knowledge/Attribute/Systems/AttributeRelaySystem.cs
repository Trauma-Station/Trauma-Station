// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Attribute.Attribute.Components;
using Content.Trauma.Shared.Knowledge.Systems;

namespace Content.Trauma.Shared.Knowledge.Attribute.Attribute.Systems;

/// <summary>
/// This class handles all the relay events for attributes
/// </summary>
public sealed partial class AttributeRelaySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AttackAttributeComponent, GetAttackModifierEvent>(OnCalculateAttack);
        SubscribeLocalEvent<DefenseAttributeComponent, GetDefenseModifierEvent>(OnCalculateDefense);
        SubscribeLocalEvent<DamageAttributeComponent, GetDamageModifierEvent>(OnCalculateDamage);
        SubscribeLocalEvent<StrengthFeatComponent, GetStrengthFeatEvent>(OnStrengthFeat);
        SubscribeLocalEvent<AgilityFeatComponent, GetAgilityFeatEvent>(OnCalculateAgility);
        SubscribeLocalEvent<DodgeAttributeComponent, GetDodgeSavingThrowEvent>(OnCalculateDodge);
        SubscribeLocalEvent<PhysicalAttributeComponent, GetPhysicalSavingThrowEvent>(OnCalculatePhysical);
        SubscribeLocalEvent<MentalAttributeComponent, GetMentalSavingThrowEvent>(OnCalculateMental);
        SubscribeLocalEvent<StrengthFeatComponent, GetCarryLimitsEvent>(OnCarry);
        SubscribeLocalEvent<MoraleAttributeComponent, GetMoraleModifierEvent>(OnCalculateMorale);
    }

    private void OnCalculateAttack(Entity<AttackAttributeComponent> ent, ref GetAttackModifierEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += AttributeSystem.LerpCurve(comp.Attribute, ent.Comp.MinX, ent.Comp.MaxX, ent.Comp.MinY, ent.Comp.MaxY);
    }

    private void OnCalculateDefense(Entity<DefenseAttributeComponent> ent, ref GetDefenseModifierEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += AttributeSystem.LerpCurve(comp.Attribute, ent.Comp.MinX, ent.Comp.MaxX, ent.Comp.MinY, ent.Comp.MaxY);
    }

    private void OnCalculateDamage(Entity<DamageAttributeComponent> ent, ref GetDamageModifierEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += AttributeSystem.LerpCurve(comp.Attribute, 1.01, 20.51, -14, 14);
    }

    private void OnStrengthFeat(Entity<StrengthFeatComponent> ent, ref GetStrengthFeatEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += AttributeSystem.LerpCurve(comp.Attribute, 1.01, 20.51, -14, 18);
    }


    private void OnCalculateAgility(Entity<AgilityFeatComponent> ent, ref GetAgilityFeatEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += AttributeSystem.LerpCurve(comp.Attribute, 3.01, 20.51, -10, 18);
    }

    private void OnCalculateDodge(Entity<DodgeAttributeComponent> ent, ref GetDodgeSavingThrowEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += AttributeSystem.LerpCurve(comp.Attribute, 3.01, 20.51, -3, 3);
    }

    private void OnCalculatePhysical(Entity<PhysicalAttributeComponent> ent, ref GetPhysicalSavingThrowEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += AttributeSystem.LerpCurve(comp.Attribute, 1.01, 22.01, -5, 6);
    }

    private void OnCalculateMental(Entity<MentalAttributeComponent> ent, ref GetMentalSavingThrowEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += AttributeSystem.LerpCurve(comp.Attribute, 1.01, 20.01, -5, 4);
    }

    private void OnCarry(Entity<StrengthFeatComponent> ent, ref GetCarryLimitsEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Lift += AttributeSystem.LerpCurve(comp.Attribute, 1.01, 20.51, 32, 675);
        args.Carry += AttributeSystem.LerpCurve(comp.Attribute, 1.01, 20.51, 15, 384);
        args.Drag += AttributeSystem.LerpCurve(comp.Attribute, 1.01, 20.51, 80, 1688);
    }

    private void OnCalculateMorale(Entity<MoraleAttributeComponent> ent, ref GetMoraleModifierEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += AttributeSystem.LerpCurve(comp.Attribute, 1.01, 22.01, -5, 6);
    }
}
