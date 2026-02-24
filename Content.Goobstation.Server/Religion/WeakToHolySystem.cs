// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Solstice <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 TheBorzoiMustConsume <197824988+TheBorzoiMustConsume@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <linebarrelerenthusiast@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Bible;
using Content.Goobstation.Shared.Religion.Nullrod;
using Content.Goobstation.Shared.Religion.Nullrod.Systems;
using Content.Shared._Shitcode.Heretic.Rituals;
using Content.Medical.Common.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Medical.Common.Targeting;

namespace Content.Goobstation.Shared.Religion;

public sealed class WeakToHolySystem : SharedWeakToHolySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly HashSet<Entity<ShouldTakeHolyComponent>> _toUpdate = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShouldTakeHolyComponent, UnholyStatusChangedEvent>(OnUnholyStatus);

        SubscribeLocalEvent<HereticRitualRuneComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<HereticRitualRuneComponent, EndCollideEvent>(OnCollideEnd);

        SubscribeLocalEvent<WeakToHolyComponent, ComponentShutdown>(OnWeakShutdown);
        SubscribeLocalEvent<WeakToHolyComponent, UnholyStatusChangedEvent>(OnWeakStatus);

        SubscribeLocalEvent<UnholyItemComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<UnholyItemComponent, GotUnequippedEvent>(OnUnquip);
        SubscribeLocalEvent<UnholyItemComponent, GotEquippedHandEvent>(OnHandEquip);
        SubscribeLocalEvent<UnholyItemComponent, GotUnequippedHandEvent>(OnHandUnequip);
    }

    private void OnHandUnequip(Entity<UnholyItemComponent> ent, ref GotUnequippedHandEvent args)
    {
        var ev = new UnholyStatusChangedEvent(args.User, ent, false);
        RaiseLocalEvent(args.User, ref ev);
    }

    private void OnHandEquip(Entity<UnholyItemComponent> ent, ref GotEquippedHandEvent args)
    {
        var ev = new UnholyStatusChangedEvent(args.User, ent, true);
        RaiseLocalEvent(args.User, ref ev);
    }

    private void OnUnquip(Entity<UnholyItemComponent> ent, ref GotUnequippedEvent args)
    {
        if (args.SlotFlags == SlotFlags.POCKET)
            return;

        var ev = new UnholyStatusChangedEvent(args.Equipee, ent, false);
        RaiseLocalEvent(args.Equipee, ref ev);
    }

    private void OnEquip(Entity<UnholyItemComponent> ent, ref GotEquippedEvent args)
    {
        if (args.SlotFlags == SlotFlags.POCKET)
            return;

        var ev = new UnholyStatusChangedEvent(args.Equipee, ent, true);
        RaiseLocalEvent(args.Equipee, ref ev);
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

    #region Holy Healing

    // Passively heal on runes
    private void OnCollide(Entity<HereticRitualRuneComponent> ent, ref StartCollideEvent args)
    {
        if (!TryComp<WeakToHolyComponent>(args.OtherEntity, out var weak))
            return;

        weak.IsColliding = true;
    }

    private void OnCollideEnd(Entity<HereticRitualRuneComponent> ent, ref EndCollideEvent args)
    {
        if (!TryComp<WeakToHolyComponent>(args.OtherEntity, out var weak))
            return;

        weak.IsColliding = false;
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

            if (!TryComp<DamageableComponent>(uid, out var damageable))
                continue;

            if (TerminatingOrDeleted(uid) || !damageable.Damage.DamageDict.TryGetValue("Holy", out _))
                continue;

            // Rune healing vs passive healing
            var healing = weakToHoly.IsColliding ? weakToHoly.HealAmount : weakToHoly.PassiveAmount;
            _damageableSystem.ChangeDamage(uid, healing, ignoreBlockers: true, targetPart: TargetBodyPart.All, splitDamage: SplitDamageBehavior.SplitEnsureAll);
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

    #endregion
}
