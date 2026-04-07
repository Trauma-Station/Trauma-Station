using System;
using System.Collections.Generic;
using System.Text;
using Content.Trauma.Common.Attribute;
using Content.Trauma.Common.Attribute.Components;
using Content.Trauma.Shared.Attribute.Components;

namespace Content.Trauma.Shared.Attribute.Systems;

/// <summary>
/// This class handles all the relay events
/// </summary>
public sealed partial class AttributeRelaySystem : EntitySystem
{
    [Dependency] private readonly SharedAttributeSystem _attribute = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AttributeHolderComponent, GetAttackModifierEvent>(_attribute.RelayEvent);
        SubscribeLocalEvent<AttackAttributeComponent, GetAttackModifierEvent>(OnCalculateAttack);

        SubscribeLocalEvent<AttributeHolderComponent, GetDefenseModifierEvent>(_attribute.RelayEvent);
        SubscribeLocalEvent<DefenseAttributeComponent, GetDefenseModifierEvent>(OnCalculateDefense);

        SubscribeLocalEvent<AttributeHolderComponent, GetDamageModifierEvent>(_attribute.RelayEvent);
        SubscribeLocalEvent<DamageAttributeComponent, GetDamageModifierEvent>(OnCalculateDamage);

        SubscribeLocalEvent<AttributeHolderComponent, GetStrengthFeatEvent>(_attribute.RelayEvent);
        SubscribeLocalEvent<StrengthFeatComponent, GetStrengthFeatEvent>(OnStrengthFeat);

        SubscribeLocalEvent<AttributeHolderComponent, GetAgilityFeatEvent>(_attribute.RelayEvent);
        SubscribeLocalEvent<AgilityFeatComponent, GetAgilityFeatEvent>(OnCalculateAgility);

        SubscribeLocalEvent<AttributeHolderComponent, GetDodgeSavingThrowEvent>(_attribute.RelayEvent);
        SubscribeLocalEvent<DodgeAttributeComponent, GetDodgeSavingThrowEvent>(OnCalculateDodge);

        SubscribeLocalEvent<AttributeHolderComponent, GetPhysicalSavingThrowEvent>(_attribute.RelayEvent);
        SubscribeLocalEvent<PhysicalAttributeComponent, GetPhysicalSavingThrowEvent>(OnCalculatePhysical);

        SubscribeLocalEvent<AttributeHolderComponent, GetMentalSavingThrowEvent>(_attribute.RelayEvent);
        SubscribeLocalEvent<MentalAttributeComponent, GetMentalSavingThrowEvent>(OnCalculateMental);

        SubscribeLocalEvent<AttributeHolderComponent, GetCarryLimitsEvent>(_attribute.RelayEvent);
        SubscribeLocalEvent<StrengthFeatComponent, GetCarryLimitsEvent>(OnCarry);

        SubscribeLocalEvent<AttributeHolderComponent, GetMoraleModifierEvent>(_attribute.RelayEvent);
        SubscribeLocalEvent<MoraleAttributeComponent, GetMoraleModifierEvent>(OnCalculateMorale);
    }

    private void OnCalculateAttack(Entity<AttackAttributeComponent> ent, ref GetAttackModifierEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += SharedAttributeSystem.LerpCurve(comp.Attribute, ent.Comp.MinX, ent.Comp.MaxX, ent.Comp.MinY, ent.Comp.MaxY);
    }

    private void OnCalculateDefense(Entity<DefenseAttributeComponent> ent, ref GetDefenseModifierEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += SharedAttributeSystem.LerpCurve(comp.Attribute, ent.Comp.MinX, ent.Comp.MaxX, ent.Comp.MinY, ent.Comp.MaxY);
    }

    private void OnCalculateDamage(Entity<DamageAttributeComponent> ent, ref GetDamageModifierEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += SharedAttributeSystem.LerpCurve(comp.Attribute, 1.01, 20.51, -7, 7);
    }

    private void OnStrengthFeat(Entity<StrengthFeatComponent> ent, ref GetStrengthFeatEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += SharedAttributeSystem.LerpCurve(comp.Attribute, 1.01, 20.51, -14, 18);
    }


    private void OnCalculateAgility(Entity<AgilityFeatComponent> ent, ref GetAgilityFeatEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += SharedAttributeSystem.LerpCurve(comp.Attribute, 3.01, 20.51, -10, 18);
    }

    private void OnCalculateDodge(Entity<DodgeAttributeComponent> ent, ref GetDodgeSavingThrowEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += SharedAttributeSystem.LerpCurve(comp.Attribute, 3.01, 20.51, -3, 3);
    }

    private void OnCalculatePhysical(Entity<PhysicalAttributeComponent> ent, ref GetPhysicalSavingThrowEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += SharedAttributeSystem.LerpCurve(comp.Attribute, 1.01, 22.01, -5, 6);
    }

    private void OnCalculateMental(Entity<MentalAttributeComponent> ent, ref GetMentalSavingThrowEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += SharedAttributeSystem.LerpCurve(comp.Attribute, 1.01, 20.01, -5, 4);
    }

    private void OnCarry(Entity<StrengthFeatComponent> ent, ref GetCarryLimitsEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Lift += SharedAttributeSystem.LerpCurve(comp.Attribute, 1.01, 20.51, 32, 675);
        args.Carry += SharedAttributeSystem.LerpCurve(comp.Attribute, 1.01, 20.51, 15, 384);
        args.Drag += SharedAttributeSystem.LerpCurve(comp.Attribute, 1.01, 20.51, 80, 1688);
    }

    private void OnCalculateMorale(Entity<MoraleAttributeComponent> ent, ref GetMoraleModifierEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += SharedAttributeSystem.LerpCurve(comp.Attribute, 1.01, 22.01, -5, 6);
    }
}
