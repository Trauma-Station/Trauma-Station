// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Movement.Systems;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Shared.MartialArts.Components;

namespace Content.Trauma.Shared.MartialArts;

/// <summary>
/// This handles temporary combat modifiers that stack while you keep fighting.
/// </summary>
public partial class MartialArtsSystem
{
    [Dependency] private MovementSpeedModifierSystem _speed = default!;
    [Dependency] private EntityQuery<NoMartialMeleeSpeedComponent> _noSpeedQuery = default!;

    private void UpdateModifiers()
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<MartialArtModifiersComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextUpdate)
                continue;

            var hadMoveSpeed = false;
            for (var i = comp.Data.Count - 1; i >= 0; i--)
            {
                if (now < comp.Data[i].EndTime)
                    continue;

                hadMoveSpeed |= (comp.Data[i].Type & MartialArtModifierType.MoveSpeed) != 0;
                comp.Data.RemoveAt(i);
            }

            DirtyField(uid, comp, nameof(MartialArtModifiersComponent.Data));
            RefreshNextUpdate((uid, comp));

            if (hadMoveSpeed && comp.User is { } user)
                _speed.RefreshMovementSpeedModifiers(user);
        }
    }

    /// <summary>
    /// Adds a modifier that will stack with any others of its type until it expires.
    /// </summary>
    public void ApplyModifier(Entity<MartialArtModifiersComponent> ent,
        MartialArtModifierType type,
        float multiplier,
        float modifier,
        TimeSpan duration,
        EntityUid? user = null)
    {
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

        ent.Comp.NextUpdate = next;
        DirtyField(ent.Owner, ent.Comp, nameof(MartialArtModifiersComponent.NextUpdate));
    }

    /// <summary>
    /// Totals every live modifier of a type, then clamps it to that type's limit.
    /// </summary>
    public (float Multiplier, float Modifier) GetModifiers(Entity<MartialArtModifiersComponent> ent,
        MartialArtModifierType type,
        bool unarmed)
    {
        var mult = 1f;
        var mod = 0f;
        foreach (var data in ent.Comp.Data)
        {
            if ((data.Type & type) == 0)
                continue;

            if ((data.Type & MartialArtModifierType.Unarmed) != 0 && !unarmed)
                continue;

            if ((data.Type & MartialArtModifierType.Armed) != 0 && unarmed)
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
        if (_noSpeedQuery.HasComp(args.Weapon))
            return;

        var (mult, mod) = GetModifiers(ent, MartialArtModifierType.AttackRate, args.Weapon == args.User);
        args.Multipliers *= mult;
        args.Rate += mod;
    }

    [SubscribeLocalEvent]
    private void OnModifyDamage(Entity<MartialArtModifiersComponent> ent, ref GetUserMeleeDamageEvent args)
    {
        var (mult, mod) = GetModifiers(ent, MartialArtModifierType.Damage, args.Weapon == ent.Comp.User);
        args.Damage *= mult;

        if (mod != 0f)
            args.Damage += new DamageSpecifier(ProtoMan.Index(ent.Comp.FlatDamageType), mod);
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
        if (args.Performer == args.Target || !TryComp<MartialArtModifiersComponent>(ent, out var modifiers))
            return;

        var velocity = _physicsQuery.TryComp(args.Performer, out var physics)
            ? physics.LinearVelocity.Length()
            : 0f;

        foreach (var rule in ent.Comp.Modifiers)
        {
            if (rule.AttackType is { } attackType && attackType != args.Type)
                continue;

            var multiplier = rule.VelocityExponent is { } exponent
                ? Math.Clamp(MathF.Pow(velocity, exponent), rule.MinMultiplier, rule.MaxMultiplier)
                : rule.Multiplier;

            ApplyModifier((ent, modifiers), rule.Type, multiplier, rule.Modifier, rule.Duration, args.Performer);
        }
    }
}
