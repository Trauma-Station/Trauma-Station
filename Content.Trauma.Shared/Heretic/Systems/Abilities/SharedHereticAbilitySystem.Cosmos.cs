// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Coordinates.Helpers;
using Content.Shared.Projectiles;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Teleportation;
using Content.Trauma.Shared.Wizard;
using Content.Trauma.Shared.Wizard.FadingTimedDespawn;
using Robust.Shared.Map;

namespace Content.Trauma.Shared.Heretic.Systems.Abilities;

public abstract partial class SharedHereticAbilitySystem
{
    [Dependency] private readonly TeleportSystem _teleport = default!;

    protected virtual void SubscribeCosmos()
    {
        SubscribeLocalEvent<EventHereticCosmicRune>(OnCosmicRune);
        SubscribeLocalEvent<StarBlastActionComponent, EventHereticStarBlast>(OnStarBlast);
        SubscribeLocalEvent<EventHereticCosmicExpansion>(OnExpansion);
        SubscribeLocalEvent<HereticAscensionCosmosEvent>(OnAscensionCosmos);

        SubscribeLocalEvent<StarBlastComponent, ProjectileHitEvent>(OnHit);
        SubscribeLocalEvent<StarBlastComponent, EntityTerminatingEvent>(OnEntityTerminating);
    }

    private void OnAscensionCosmos(HereticAscensionCosmosEvent args)
    {
        _eye.SetDrawFov(args.Heretic, args.Negative);
    }

    private void OnExpansion(EventHereticCosmicExpansion args)
    {
        if (!TryUseAbility(args))
            return;

        var ent = args.Performer;

        var coords = Transform(ent).Coordinates;

        Heretic.TryGetHereticComponent(ent, out var heretic, out _);
        var strength = heretic is { CurrentPath: HereticPath.Cosmos } ? heretic.PathStage : 10;

        _starMark.ApplyStarMarkInRange(coords, ent, args.Range);
        _starMark.SpawnCosmicFields(coords, 2, strength);

        PredictedSpawnAtPosition(args.Effect, coords);

        if (heretic is { Ascended: true, CurrentPath: HereticPath.Cosmos })
        {
            _starMark.SpawnCosmicFieldLine(coords, DirectionFlag.North, -4, 4, 3, strength);
            _starMark.SpawnCosmicFieldLine(coords, DirectionFlag.East, -4, 4, 3, strength);
        }
    }

    private void OnStarBlast(Entity<StarBlastActionComponent> ent, ref EventHereticStarBlast args)
    {
        if (!TryUseAbility(args, false))
            return;

        var user = args.Performer;

        Heretic.TryGetHereticComponent(user, out var heretic, out _);
        var strength = heretic is { CurrentPath: HereticPath.Cosmos } ? heretic.PathStage : 10;

        if (Exists(ent.Comp.Projectile))
        {
            _actions.SetIfBiggerCooldown(args.Action.AsNullable(), ent.Comp.Cooldown);
            if (!_teleport.CanTeleport(user)) // don't want to apply star mark if teleporting is prevented
                return;

            var newCoords = Transform(ent.Comp.Projectile).Coordinates;
            var oldCoords = Transform(user).Coordinates;

            PredictedSpawnAtPosition(ent.Comp.Effect, oldCoords);
            PullVictims(user, oldCoords, strength);
            _teleport.Teleport(user, newCoords, ent.Comp.Sound, user);
            PredictedSpawnAtPosition(ent.Comp.Effect, newCoords);
            PullVictims(user, newCoords, strength);

            PredictedQueueDel(ent.Comp.Projectile);

            ent.Comp.Projectile = EntityUid.Invalid;
            Dirty(ent);

            // Don't do args.Handled = true because it resets cooldown

            if (_net.IsServer)
                RaiseNetworkEvent(new StopTargetingEvent(), user);

            return;
        }

        if (!args.Target.IsValid(EntityManager))
            return;

        args.Handled = true;

        ent.Comp.Projectile = ShootProjectileSpell(args.Performer,
            args.Target,
            args.Projectile,
            args.ProjectileSpeed,
            args.Entity);
        EnsureComp<CosmicTrailComponent>(ent.Comp.Projectile).Strength = strength;
        EnsureComp<StarBlastComponent>(ent.Comp.Projectile).Action = args.Action;
        Dirty(ent);
    }

    private void OnEntityTerminating(Entity<StarBlastComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.Action == EntityUid.Invalid || TerminatingOrDeleted(ent.Comp.Action) ||
            !TryComp(ent.Comp.Action, out StarBlastActionComponent? action))
            return;

        action.Projectile = EntityUid.Invalid;
        Dirty(ent.Comp.Action, action);
    }

    private void OnHit(Entity<StarBlastComponent> ent, ref ProjectileHitEvent args)
    {
        var coords = Transform(ent).Coordinates;
        _starMark.ApplyStarMarkInRange(coords, args.Shooter, ent.Comp.StarMarkRadius);

        _stun.KnockdownOrStun(args.Target, ent.Comp.KnockdownTime);
    }

    private void PullVictims(EntityUid user, EntityCoordinates coords, int strength)
    {
        foreach (var mob in GetNearbyPeople(user, 2f, HereticPath.Cosmos, coords))
        {
            if (_starMark.TryApplyStarMark(mob.AsNullable()))
                _throw.TryThrow(mob, coords);
        }
        _starMark.SpawnCosmicFields(coords, 1, strength);
    }

    private void OnCosmicRune(EventHereticCosmicRune args)
    {
        if (!TryComp(args.Action, out HereticCosmicRuneActionComponent? runeAction))
            return;

        if (!TryUseAbility(args, false))
            return;

        var coords = Transform(args.Performer).Coordinates.SnapToGrid(EntityManager, _mapMan);

        // No placing runes on top of runes
        if (Lookup.GetEntitiesInRange<HereticCosmicRuneComponent>(coords, 0.4f).Count > 0)
        {
            Popup.PopupClient(Loc.GetString("heretic-ability-fail-tile-occupied"), args.Performer, args.Performer);
            return;
        }

        args.Handled = true;

        if (_net.IsClient)
            return;

        var firstRuneResolved = Exists(runeAction.FirstRune);
        var secondRuneResolved = Exists(runeAction.SecondRune);

        if (firstRuneResolved && secondRuneResolved)
        {
            EnsureComp<FadingTimedDespawnComponent>(runeAction.FirstRune!.Value).Lifetime = 0f;
            var newRune = Spawn(args.Rune, coords);
            _transform.AttachToGridOrMap(newRune);
            var newRuneComp = EnsureComp<HereticCosmicRuneComponent>(newRune);
            var secondRuneComp = EnsureComp<HereticCosmicRuneComponent>(runeAction.SecondRune!.Value);
            newRuneComp.LinkedRune = runeAction.SecondRune.Value;
            secondRuneComp.LinkedRune = newRune;
            DirtyField(newRune, newRuneComp, nameof(HereticCosmicRuneComponent.LinkedRune));
            DirtyField(runeAction.SecondRune.Value, secondRuneComp, nameof(HereticCosmicRuneComponent.LinkedRune));
            runeAction.FirstRune = runeAction.SecondRune.Value;
            runeAction.SecondRune = newRune;
            return;
        }

        if (!firstRuneResolved)
        {
            var newRune = Spawn(args.Rune, coords);
            _transform.AttachToGridOrMap(newRune);
            runeAction.FirstRune = newRune;

            if (!secondRuneResolved)
                return;

            var newRuneComp = EnsureComp<HereticCosmicRuneComponent>(newRune);
            var secondRuneComp = EnsureComp<HereticCosmicRuneComponent>(runeAction.SecondRune!.Value);
            newRuneComp.LinkedRune = runeAction.SecondRune.Value;
            secondRuneComp.LinkedRune = newRune;
            DirtyField(newRune, newRuneComp, nameof(HereticCosmicRuneComponent.LinkedRune));
            DirtyField(runeAction.SecondRune.Value, secondRuneComp, nameof(HereticCosmicRuneComponent.LinkedRune));
            return;
        }


        if (!secondRuneResolved)
        {
            var newRune = Spawn(args.Rune, coords);
            _transform.AttachToGridOrMap(newRune);
            runeAction.SecondRune = newRune;

            if (!firstRuneResolved)
                return;

            var newRuneComp = EnsureComp<HereticCosmicRuneComponent>(newRune);
            var firstRuneComp = EnsureComp<HereticCosmicRuneComponent>(runeAction.FirstRune!.Value);
            newRuneComp.LinkedRune = runeAction.FirstRune.Value;
            firstRuneComp.LinkedRune = newRune;
            DirtyField(newRune, newRuneComp, nameof(HereticCosmicRuneComponent.LinkedRune));
            DirtyField(runeAction.FirstRune.Value, firstRuneComp, nameof(HereticCosmicRuneComponent.LinkedRune));
        }
    }
}
