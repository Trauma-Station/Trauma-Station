// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Attribute;
using Content.Trauma.Common.Attribute.Components;
using Content.Trauma.Shared.Attribute.Components;

namespace Content.Trauma.Shared.Attribute.Systems;

/// <summary>
/// Handles all wisdom related bullshit.
/// </summary>
public sealed partial class WisdomSystem : EntitySystem
{
    [Dependency] private readonly SharedAttributeSystem _attribute = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AttributeHolderComponent, GetAttackModifierEvent>(_attribute.RelayEvent);
        SubscribeLocalEvent<AttackAttributeComponent, GetAttackModifierEvent>(OnCalculateAttack);
        SubscribeLocalEvent<AttributeHolderComponent, GetDefenseModifierEvent>(_attribute.RelayEvent);
        SubscribeLocalEvent<DefenseAttributeComponent, GetDefenseModifierEvent>(OnCalculateDefense);
        SubscribeLocalEvent<AttributeHolderComponent, GetMentalSavingThrowEvent>(_attribute.RelayEvent);
        SubscribeLocalEvent<MentalAttributeComponent, GetMentalSavingThrowEvent>(OnCalculateMental);
    }

    private void OnCalculateAttack(Entity<AttackAttributeComponent> ent, ref GetAttackModifierEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += SharedAttributeSystem.LerpCurve(comp.Attribute, 1.01, 20.01, -5, 3);
    }

    private void OnCalculateDefense(Entity<DefenseAttributeComponent> ent, ref GetDefenseModifierEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += SharedAttributeSystem.LerpCurve(comp.Attribute, 1.01, 20.01, -4, 3);
    }

    private void OnCalculateMental(Entity<MentalAttributeComponent> ent, ref GetMentalSavingThrowEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += SharedAttributeSystem.LerpCurve(comp.Attribute, 1.01, 20.01, -5, 4);
    }
}
