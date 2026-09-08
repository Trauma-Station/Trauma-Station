// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Movement.Systems;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Shared.MartialArts.Components;

namespace Content.Trauma.Shared.MartialArts;

/// <summary>
/// This handles temporary combat modifiers.
/// </summary>
public partial class MartialArtsSystem
{
    [Dependency] private MovementSpeedModifierSystem _speed = default!;
    [Dependency] private EntityQuery<MartialArtModifiersComponent> _query = default!;

    private void UpdateModifiers()
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<MartialArtModifiersComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextUpdate)
                continue;

            var hadMoveSpeed = false;
            var removed = false;
            for (var i = comp.Data.Count - 1; i >= 0; i--)
            {
                if (now < comp.Data[i].EndTime)
                    continue;

                hadMoveSpeed |= comp.Data[i].Has(MartialArtModifierType.MoveSpeed);
                comp.Data.RemoveSwap(i);
                removed = true;
            }

            if (removed)
                DirtyField(uid, comp, nameof(MartialArtModifiersComponent.Data));

            RefreshNextUpdate((uid, comp));

            if (hadMoveSpeed && comp.User is { } user)
                _speed.RefreshMovementSpeedModifiers(user);
        }
    }

    public void ApplyModifier(Entity<MartialArtModifiersComponent> ent,
        MartialArtModifierType type,
        float multiplier,
        float modifier,
        TimeSpan duration,
        EntityUid? user = null)
    {
        // standing still gives a multiplier of exactly 1, don't store an entry that does nothing
        if (Math.Abs(multiplier - 1f) < 0.001f && Math.Abs(modifier) < 0.001f || duration <= TimeSpan.Zero)
            return;

        ent.Comp.Data.Add(new MartialArtModifierData
        {
            Type = type,
            Multiplier = multiplier,
            Modifier = modifier,
            EndTime = _timing.CurTime + duration,
        });

        DirtyField(ent.Owner, ent.Comp, nameof(MartialArtModifiersComponent.Data));

        if (user is { })
        {
            ent.Comp.User = user;
            DirtyField(ent.Owner, ent.Comp, nameof(MartialArtModifiersComponent.User));
        }

        RefreshNextUpdate(ent);

        if ((type & MartialArtModifierType.MoveSpeed) != 0 && ent.Comp.User is { } mob)
            _speed.RefreshMovementSpeedModifiers(mob);
    }

    private void RefreshNextUpdate(Entity<MartialArtModifiersComponent> ent)
    {
        var next = TimeSpan.MaxValue;
        foreach (var data in ent.Comp.Data)
        {
            if (data.EndTime < next)
                next = data.EndTime;
        }

        if (ent.Comp.NextUpdate == next)
            return;

        ent.Comp.NextUpdate = next;
        DirtyField(ent.Owner, ent.Comp, nameof(MartialArtModifiersComponent.NextUpdate));
    }

    public (float Multiplier, float Modifier) GetModifiers(Entity<MartialArtModifiersComponent> ent,
        MartialArtModifierType type,
        bool unarmed)
    {
        var mult = 1f;
        var mod = 0f;
        foreach (var data in ent.Comp.Data)
        {
            if (!data.Has(type))
                continue;

            if (data.Has(MartialArtModifierType.Unarmed) && !unarmed)
                continue;

            if (data.Has(MartialArtModifierType.Armed) && unarmed)
                continue;

            mult *= data.Multiplier;
            mod += data.Modifier;
        }

        if (!ent.Comp.Limits.TryGetValue(type, out var limit))
            return (mult, mod);

        return (Math.Clamp(mult, limit.MinMultiplier, limit.MaxMultiplier),
            Math.Clamp(mod, limit.MinModifier, limit.MaxModifier));
    }

    [SubscribeLocalEvent]
    private void OnModifyAttackRate(Entity<MartialArtModifiersComponent> ent, ref GetMeleeAttackRateEvent args)
    {
        if (args.Weapon != args.User)
            return; // don't speed up actual weapons just punches

        var (mult, mod) = GetModifiers(ent, MartialArtModifierType.AttackRate, args.Weapon == args.User);
        args.Multipliers *= mult;
        args.Rate += mod;
    }

    [SubscribeLocalEvent]
    private void OnModifyDamage(Entity<MartialArtModifiersComponent> ent, ref GetUserMeleeDamageEvent args)
    {
        var (mult, mod) = GetModifiers(ent, MartialArtModifierType.Damage, args.Weapon == ent.Comp.User);
        args.Damage *= mult;

        if (mod == 0f)
            return;

        var dict = args.Damage.DamageDict;
        var type = ent.Comp.FlatDamageType;
        if (!dict.TryAdd(type, mod))
            dict[type] += mod;
    }

    [SubscribeLocalEvent]
    private void OnModifyMoveSpeed(Entity<MartialArtModifiersComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        var (mult, _) = GetModifiers(ent, MartialArtModifierType.MoveSpeed, true);
        args.ModifySpeed(mult);
    }

    [SubscribeLocalEvent]
    private void OnComboAttackModifier(Entity<ComboAttackModifierComponent> ent, ref ComboAttackPerformedEvent args)
    {
        // self shoves are how you kick up, they should not build momentum
        if (args.Performer == args.Target || !_query.TryComp(ent.Owner, out var modifiers))
            return;

        foreach (var rule in ent.Comp.Modifiers)
        {
            if (rule.AttackTypes is { } types && !types.Contains(args.Type))
                continue;

            if (rule.UnarmedOnly && args.Weapon != args.Performer)
                continue;

            var ev = rule.Multiplier;
            ev.Reset(args.Performer);
            RaiseLocalEvent(args.Performer, (object) ev);

            ApplyModifier((ent, modifiers), rule.Type, ev.Multiplier, rule.Modifier, rule.Duration, args.Performer);
        }
    }
}
