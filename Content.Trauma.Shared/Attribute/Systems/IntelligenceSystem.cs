// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Attribute;
using Content.Trauma.Common.Attribute.Components;
using Content.Trauma.Shared.Attribute.Components;

namespace Content.Trauma.Shared.Attribute.Systems;

/// <summary>
/// Handles all intelligence related bullshit.
/// </summary>
public sealed partial class IntelligenceSystem : EntitySystem
{
    [Dependency] private readonly SharedAttributeSystem _attribute = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AttributeHolderComponent, GetAttackModifierEvent>(_attribute.RelayEvent);
        SubscribeLocalEvent<AttackAttributeComponent, GetAttackModifierEvent>(OnCalculateAttack);
    }

    private void OnCalculateAttack(Entity<AttackAttributeComponent> ent, ref GetAttackModifierEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += SharedAttributeSystem.LerpCurve(comp.Attribute, 1.01, 20.01, -5, 3);
    }
}
