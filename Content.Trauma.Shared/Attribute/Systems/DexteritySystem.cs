// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Attribute;
using Content.Trauma.Common.Attribute.Components;
using Content.Trauma.Shared.Attribute.Components;

namespace Content.Trauma.Shared.Attribute.Systems;

/// <summary>
/// Handles all dexterity related bullshit.
/// </summary>
public sealed class DexteritySystem : EntitySystem
{
    [Dependency] private readonly SharedAttributeSystem _attribute = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AttributeHolderComponent, GetAttackModifierEvent>(_attribute.RelayEvent);
        SubscribeLocalEvent<AttackAttributeComponent, GetAttackModifierEvent>(OnCalculateAttack);
        SubscribeLocalEvent<AttributeHolderComponent, GetDefenseModifierEvent>(_attribute.RelayEvent);
        SubscribeLocalEvent<DefenseAttributeComponent, GetDefenseModifierEvent>(OnCalculateDefense);
        SubscribeLocalEvent<AttributeHolderComponent, GetDodgeSavingThrowEvent>(_attribute.RelayEvent);
        SubscribeLocalEvent<DodgeAttributeComponent, GetDodgeSavingThrowEvent>(OnCalculateDodge);
        SubscribeLocalEvent<AttributeHolderComponent, GetAgilityFeatEvent>(_attribute.RelayEvent);
        SubscribeLocalEvent<AgilityFeatComponent, GetAgilityFeatEvent>(OnCalculateAgility);
    }

    private void OnCalculateAttack(Entity<AttackAttributeComponent> ent, ref GetAttackModifierEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += SharedAttributeSystem.LerpCurve(comp.Attribute, 3.01, 20.51, -4, 5);
    }

    private void OnCalculateDefense(Entity<DefenseAttributeComponent> ent, ref GetDefenseModifierEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += SharedAttributeSystem.LerpCurve(comp.Attribute, 3.01, 20.51, -5, 7);
    }

    private void OnCalculateDodge(Entity<DodgeAttributeComponent> ent, ref GetDodgeSavingThrowEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += SharedAttributeSystem.LerpCurve(comp.Attribute, 3.01, 20.51, -3, 3);
    }

    private void OnCalculateAgility(Entity<AgilityFeatComponent> ent, ref GetAgilityFeatEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += SharedAttributeSystem.LerpCurve(comp.Attribute, 3.01, 20.51, -10, 18);
    }
}
