// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Weapons.DelayedKnockdown;
using Content.Goobstation.Shared.Clothing;
using Content.Shared.Armor;
using Content.Shared.Damage.Events;
using Content.Shared.Inventory;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Timing;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Weapons.DelayedKnockdown;

public sealed partial class DelayedKnockdownOnHitSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private UseDelaySystem _delay = default!;
    [Dependency] private EntityQuery<StandingStateComponent> _standingQuery = default!;

    [SubscribeLocalEvent]
    private void OnExamine(Entity<ModifyDelayedKnockdownComponent> ent, ref ArmorExamineEvent args)
    {
        var comp = ent.Comp;

        if (comp.Cancel)
        {
            args.Msg.PushNewline();
            args.Msg.AddMarkupOrThrow(Loc.GetString("armor-examine-cancel-delayed-knockdown"));
            return;
        }

        if (comp.DelayDelta != TimeSpan.Zero)
        {
            args.Msg.PushNewline();
            args.Msg.AddMarkupOrThrow(Loc.GetString("armor-examine-modify-delayed-knockdown-delay",
                ("amount", Math.Abs(comp.DelayDelta.TotalSeconds)),
                ("deltasign", Math.Sign(comp.DelayDelta.TotalSeconds))));
        }

        if (comp.KnockdownTimeDelta != TimeSpan.Zero)
        {
            args.Msg.PushNewline();
            args.Msg.AddMarkupOrThrow(Loc.GetString("armor-examine-modify-delayed-knockdown-time",
                ("amount", Math.Abs(comp.KnockdownTimeDelta.TotalSeconds)),
                ("deltasign", Math.Sign(comp.KnockdownTimeDelta.TotalSeconds))));
        }
    }

    [SubscribeLocalEvent]
    private void OnInventoryAttempt(Entity<ModifyDelayedKnockdownComponent> ent, ref InventoryRelayedEvent<DelayedKnockdownAttemptEvent> args)
    {
        OnAttempt(ent, ref args.Args);
    }

    [SubscribeLocalEvent]
    private void OnAttempt(Entity<ModifyDelayedKnockdownComponent> ent, ref DelayedKnockdownAttemptEvent args)
    {
        var comp = ent.Comp;
        if (comp.Cancel)
        {
            args.Cancelled = true;
            return;
        }

        args.DelayDelta += comp.DelayDelta;
        args.KnockdownTimeDelta += comp.KnockdownTimeDelta;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<DelayedKnockdownComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextKnockdown)
                continue;

            _stun.TryKnockdown(uid, comp.KnockdownTime, comp.Refresh, stunOnFail: false);

            RemCompDeferred(uid, comp);
        }
    }

    [SubscribeLocalEvent]
    private void OnHit(Entity<DelayedKnockdownOnHitComponent> ent, ref StaminaMeleeHitEvent args)
    {
        if (args.HitList.Count == 0)
            return;

        var (uid, comp) = ent;

        if (!comp.ApplyOnHeavyAttack && args.WideSwing)
            return;

        if (TryComp(uid, out UseDelayComponent? delay))
            _delay.TryResetDelay((uid, delay), id: comp.UseDelay);

        foreach (var (hit, _) in args.HitList)
        {
            if (!_standingQuery.HasComp(hit))
                continue;

            var ev = new DelayedKnockdownAttemptEvent();
            RaiseLocalEvent(hit, ref ev);
            if (ev.Cancelled)
                continue;

            var delayed = EnsureComp<DelayedKnockdownComponent>(hit);
            if (delayed.Started == TimeSpan.Zero)
                delayed.Started = _timing.CurTime;
            // only extend delays and time if it's already there so it can't infinitely stack
            delayed.Delay = MathHelper.Min(comp.Delay + ev.DelayDelta, delayed.Delay);
            delayed.NextKnockdown = delayed.Started + delayed.Delay;
            delayed.KnockdownTime = MathHelper.Max(comp.KnockdownTime + ev.KnockdownTimeDelta, delayed.KnockdownTime);
            delayed.Refresh &= comp.Refresh;
            Dirty(hit, delayed);
        }
    }
}
