// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Religion;
using Content.Goobstation.Shared.Religion.Nullrod;
using Content.Goobstation.Shared.Religion.Nullrod.Systems;
using Content.Medical.Common.Damage;
using Content.Shared.Damage.Systems;
using Robust.Shared.Timing;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Medical.Common.Targeting;

namespace Content.Goobstation.Shared.Religion;

public sealed class WeakToHolySystem : SharedWeakToHolySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly HashSet<Entity<ShouldTakeHolyComponent>> _toUpdate = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShouldTakeHolyComponent, UnholyStatusChangedEvent>(OnUnholyStatus);

        SubscribeLocalEvent<WeakToHolyComponent, ComponentShutdown>(OnWeakShutdown);
        SubscribeLocalEvent<WeakToHolyComponent, UnholyStatusChangedEvent>(OnWeakStatus);

        SubscribeLocalEvent<UnholyItemComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<UnholyItemComponent, GotUnequippedEvent>(OnUnquip);
        SubscribeLocalEvent<UnholyItemComponent, GotEquippedHandEvent>(OnHandEquip);
        SubscribeLocalEvent<UnholyItemComponent, GotUnequippedHandEvent>(OnHandUnequip);
        SubscribeLocalEvent<UnholyItemComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<UnholyItemComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<UnholyItemComponent> ent, ref ComponentShutdown args)
    {
        var parent = Transform(ent).ParentUid;
        if (TerminatingOrDeleted(parent) || !HasComp<WeakToHolyComponent>(parent))
            return;

        ChangeUnholyStatus(ent, parent, false);
    }

    private void OnStartup(Entity<UnholyItemComponent> ent, ref ComponentStartup args)
    {
        var parent = Transform(ent).ParentUid;
        if (TerminatingOrDeleted(parent) || !HasComp<WeakToHolyComponent>(parent))
            return;

        ChangeUnholyStatus(ent, parent, true);
    }

    private void OnHandUnequip(Entity<UnholyItemComponent> ent, ref GotUnequippedHandEvent args)
    {
        ChangeUnholyStatus(ent, args.User, false);
    }

    private void OnHandEquip(Entity<UnholyItemComponent> ent, ref GotEquippedHandEvent args)
    {
        ChangeUnholyStatus(ent, args.User, true);
    }

    private void OnUnquip(Entity<UnholyItemComponent> ent, ref GotUnequippedEvent args)
    {
        if (args.SlotFlags == SlotFlags.POCKET)
            return;

        ChangeUnholyStatus(ent, args.Equipee, false);
    }

    private void OnEquip(Entity<UnholyItemComponent> ent, ref GotEquippedEvent args)
    {
        if (args.SlotFlags == SlotFlags.POCKET)
            return;

        ChangeUnholyStatus(ent, args.Equipee, true);
    }

    private void ChangeUnholyStatus(EntityUid source, EntityUid user, bool status)
    {
        var ev = new UnholyStatusChangedEvent(user, source, status);
        RaiseLocalEvent(user, ref ev);
    }

    private void OnUnholyStatus(Entity<ShouldTakeHolyComponent> ent, ref UnholyStatusChangedEvent args)
    {
        if (args.Added)
        {
            ent.Comp.Sources.Add(args.Source);
            return;
        }

        if (ent.Owner == args.Source)
        {
            var ev = new UserShouldTakeHolyEvent(ent);
            RaiseLocalEvent(ent, ref ev, true);
            if (!ev.WeakToHoly)
            {
                RemCompDeferred<WeakToHolyComponent>(ent);
                return;
            }

            if (!ev.ShouldTakeHoly)
                ent.Comp.Sources.Remove(args.Source);
            else
                return;
        }
        else
            ent.Comp.Sources.Remove(args.Source);

        _toUpdate.Add(ent);
    }

    private void OnWeakStatus(Entity<WeakToHolyComponent> ent, ref UnholyStatusChangedEvent args)
    {
        if (!args.Added || HasComp<ShouldTakeHolyComponent>(ent))
            return;

        var comp = AddComp<ShouldTakeHolyComponent>(ent);
        comp.Sources.Add(args.Source);
        Dirty(ent, comp);
    }

    private void OnWeakShutdown(Entity<WeakToHolyComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        RemCompDeferred<ShouldTakeHolyComponent>(ent);
        RemCompDeferred<AlwaysTakeHolyComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Holy damage healing.
        var query = EntityQueryEnumerator<WeakToHolyComponent>();
        while (query.MoveNext(out var uid, out var weakToHoly))
        {
            if (weakToHoly.NextPassiveHealTick > _timing.CurTime)
                continue;
            weakToHoly.NextPassiveHealTick = _timing.CurTime + weakToHoly.HealTickDelay;

            var damage = _damageable.GetAllDamage(uid);
            if (TerminatingOrDeleted(uid) || damage.DamageDict.GetValueOrDefault("Holy") <= 0)
                continue;

            // Rune healing vs passive healing
            var healing = weakToHoly.IsColliding ? weakToHoly.HealAmount : weakToHoly.PassiveAmount;
            _damageable.ChangeDamage(uid, healing, ignoreBlockers: true, targetPart: TargetBodyPart.All, splitDamage: SplitDamageBehavior.SplitEnsureAll);
        }

        if (_toUpdate.Count == 0)
            return;

        foreach (var ent in _toUpdate)
        {
            if (ent.Comp.Sources.Count == 0)
                RemCompDeferred(ent, ent.Comp);
        }

        _toUpdate.Clear();
    }
}
