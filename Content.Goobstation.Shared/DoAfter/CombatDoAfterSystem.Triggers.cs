// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.DoAfter;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Ensnaring.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;

namespace Content.Goobstation.Shared.DoAfter;

public sealed partial class CombatDoAfterSystem
{
    [Dependency] private SharedStunSystem _stun = default!;

    private void InitializeTriggers()
    {
        SubscribeLocalEvent<CombatDoAfterComponent, ThrownEvent>(OnThrow);

        SubscribeLocalEvent<EnsnaringComponent, CombatDoAfterThrownEvent>(OnEnsnaringThrow);

        SubscribeLocalEvent<EnsnaringKnockdownComponent, EnsnaredEvent>(OnEnsnared);
        SubscribeLocalEvent<EnsnaringKnockdownComponent, StopThrowEvent>(OnStopThrow);
    }

    private void OnStopThrow(Entity<EnsnaringKnockdownComponent> ent, ref StopThrowEvent args)
    {
        RemCompDeferred(ent.Owner, ent.Comp);
    }

    private void OnEnsnared(Entity<EnsnaringKnockdownComponent> ent, ref EnsnaredEvent args)
    {
        _stun.TryKnockdown(args.Target, time: null); // until the target manually gets up
        RemCompDeferred(ent.Owner, ent.Comp);
    }

    private void OnEnsnaringThrow(Entity<EnsnaringComponent> ent, ref CombatDoAfterThrownEvent args)
    {
        EnsureComp<EnsnaringKnockdownComponent>(ent);
    }

    private void OnThrow(Entity<CombatDoAfterComponent> ent, ref ThrownEvent args)
    {
        if (args.User == null)
            return;

        if (ent.Comp.Trigger is not CombatDoAfterThrownEvent thrownEvent)
            return;

        if (CheckDoAfter(ent, args.User.Value, null))
            RaiseLocalEvent(ent, (object) thrownEvent);

        TryCancelDoAfter(ent);
    }
}
