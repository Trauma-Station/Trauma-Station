// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Attribute;
using Content.Trauma.Common.Attribute.Components;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Attribute.Attribute.Components;
using Content.Trauma.Shared.Knowledge.Systems;

namespace Content.Trauma.Shared.Knowledge.Attribute.Attribute.Systems;

/// <summary>
/// This class handles all the relay events
/// </summary>
public sealed partial class AttributeRelaySystem : EntitySystem
{
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, GetAttackModifierEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<AttackAttributeComponent, GetAttackModifierEvent>(OnCalculateAttack);

        SubscribeLocalEvent<KnowledgeHolderComponent, GetDefenseModifierEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<DefenseAttributeComponent, GetDefenseModifierEvent>(OnCalculateDefense);

        SubscribeLocalEvent<KnowledgeHolderComponent, GetDamageModifierEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<DamageAttributeComponent, GetDamageModifierEvent>(OnCalculateDamage);

        SubscribeLocalEvent<KnowledgeHolderComponent, GetStrengthFeatEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<StrengthFeatComponent, GetStrengthFeatEvent>(OnStrengthFeat);

        SubscribeLocalEvent<KnowledgeHolderComponent, GetAgilityFeatEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<AgilityFeatComponent, GetAgilityFeatEvent>(OnCalculateAgility);

        SubscribeLocalEvent<KnowledgeHolderComponent, GetDodgeSavingThrowEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<DodgeAttributeComponent, GetDodgeSavingThrowEvent>(OnCalculateDodge);

        SubscribeLocalEvent<KnowledgeHolderComponent, GetPhysicalSavingThrowEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<PhysicalAttributeComponent, GetPhysicalSavingThrowEvent>(OnCalculatePhysical);

        SubscribeLocalEvent<KnowledgeHolderComponent, GetMentalSavingThrowEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<MentalAttributeComponent, GetMentalSavingThrowEvent>(OnCalculateMental);

        SubscribeLocalEvent<KnowledgeHolderComponent, GetCarryLimitsEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<StrengthFeatComponent, GetCarryLimitsEvent>(OnCarry);

        SubscribeLocalEvent<KnowledgeHolderComponent, GetMoraleModifierEvent>(_knowledge.RelayEvent);
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

        args.Mod += AttributeSystem.LerpCurve(comp.Attribute, 1.01, 20.51, -7, 7);
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
