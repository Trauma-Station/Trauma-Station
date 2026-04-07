// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Content.Trauma.Common.Attribute;
using Content.Trauma.Common.Attribute.Components;
using Content.Trauma.Common.Cuffs;
using Content.Trauma.Shared.Attribute.Components;

namespace Content.Trauma.Shared.Attribute.Systems;

/// <summary>
/// Handles all strength related bullshit.
/// </summary>
public sealed class StrengthSystem : EntitySystem
{
    [Dependency] private readonly SharedAttributeSystem _attribute = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AttributeHolderComponent, GetDamageModifierEvent>(_attribute.RelayEvent);
        SubscribeLocalEvent<DamageAttributeComponent, GetDamageModifierEvent>(OnCalculateDamage);
        SubscribeLocalEvent<AttributeHolderComponent, GetStrengthFeatEvent>(_attribute.RelayEvent);
        SubscribeLocalEvent<StrengthFeatComponent, GetStrengthFeatEvent>(OnStrengthFeat);
        SubscribeLocalEvent<AttributeHolderComponent, GetCarryLimitsEvent>(_attribute.RelayEvent);
        SubscribeLocalEvent<StrengthFeatComponent, GetCarryLimitsEvent>(OnCarry);

        // Actual Gameplay Methods
        SubscribeLocalEvent<AttributeHolderComponent, InstantUncuffEvent>(OnUncuff);
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

    private void OnCarry(Entity<StrengthFeatComponent> ent, ref GetCarryLimitsEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Lift += SharedAttributeSystem.LerpCurve(comp.Attribute, 1.01, 20.51, 32, 675);
        args.Carry += SharedAttributeSystem.LerpCurve(comp.Attribute, 1.01, 20.51, 15, 384);
        args.Drag += SharedAttributeSystem.LerpCurve(comp.Attribute, 1.01, 20.51, 80, 1688);
    }

    private void OnUncuff(Entity<AttributeHolderComponent> ent, ref InstantUncuffEvent args)
    {
        var selfEv = new GetStrengthFeatEvent();
        var cuffsEv = new GetStrengthFeatEvent();

        var ev = new OnAttributeOpposedContest(uid => RaiseLocalEvent(uid, ref selfEv), uid => RaiseLocalEvent(uid, ref cuffsEv), () => (selfEv.Mod, cuffsEv.Mod), args.Cuff);

        RaiseLocalEvent(ent, ref ev);
        if (ev.Failed)
            return;

        args.CuffsBroken = true;
    }
}
