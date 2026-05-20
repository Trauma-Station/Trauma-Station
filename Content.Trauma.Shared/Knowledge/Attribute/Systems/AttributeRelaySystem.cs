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
        SubscribeLocalEvent<LiftAttributeComponent, GetCarryLimitsEvent>(OnLift);
        SubscribeLocalEvent<CarryAttributeComponent, GetCarryLimitsEvent>(OnCarry);
        SubscribeLocalEvent<DragAttributeComponent, GetCarryLimitsEvent>(OnDrag);
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

        args.Mod += AttributeSystem.LerpCurve(comp.Attribute, ent.Comp.MinX, ent.Comp.MaxX, ent.Comp.MinY, ent.Comp.MaxY);
    }

    private void OnStrengthFeat(Entity<StrengthFeatComponent> ent, ref GetStrengthFeatEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += AttributeSystem.LerpCurve(comp.Attribute, ent.Comp.MinX, ent.Comp.MaxX, ent.Comp.MinY, ent.Comp.MaxY);
    }


    private void OnCalculateAgility(Entity<AgilityFeatComponent> ent, ref GetAgilityFeatEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += AttributeSystem.LerpCurve(comp.Attribute, ent.Comp.MinX, ent.Comp.MaxX, ent.Comp.MinY, ent.Comp.MaxY);
    }

    private void OnCalculateDodge(Entity<DodgeAttributeComponent> ent, ref GetDodgeSavingThrowEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += AttributeSystem.LerpCurve(comp.Attribute, ent.Comp.MinX, ent.Comp.MaxX, ent.Comp.MinY, ent.Comp.MaxY);
    }

    private void OnCalculatePhysical(Entity<PhysicalAttributeComponent> ent, ref GetPhysicalSavingThrowEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += AttributeSystem.LerpCurve(comp.Attribute, ent.Comp.MinX, ent.Comp.MaxX, ent.Comp.MinY, ent.Comp.MaxY);
    }

    private void OnCalculateMental(Entity<MentalAttributeComponent> ent, ref GetMentalSavingThrowEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += AttributeSystem.LerpCurve(comp.Attribute, ent.Comp.MinX, ent.Comp.MaxX, ent.Comp.MinY, ent.Comp.MaxY);
    }

    private void OnLift(Entity<LiftAttributeComponent> ent, ref GetCarryLimitsEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Lift += AttributeSystem.LerpCurve(comp.Attribute, ent.Comp.MinX, ent.Comp.MaxX, ent.Comp.MinY, ent.Comp.MaxY);
    }

    private void OnCarry(Entity<CarryAttributeComponent> ent, ref GetCarryLimitsEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Carry += AttributeSystem.LerpCurve(comp.Attribute, ent.Comp.MinX, ent.Comp.MaxX, ent.Comp.MinY, ent.Comp.MaxY);
    }

    private void OnDrag(Entity<DragAttributeComponent> ent, ref GetCarryLimitsEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Drag += AttributeSystem.LerpCurve(comp.Attribute, ent.Comp.MinX, ent.Comp.MaxX, ent.Comp.MinY, ent.Comp.MaxY);
    }

    private void OnCalculateMorale(Entity<MoraleAttributeComponent> ent, ref GetMoraleModifierEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += AttributeSystem.LerpCurve(comp.Attribute, ent.Comp.MinX, ent.Comp.MaxX, ent.Comp.MinY, ent.Comp.MaxY);
    }
}
