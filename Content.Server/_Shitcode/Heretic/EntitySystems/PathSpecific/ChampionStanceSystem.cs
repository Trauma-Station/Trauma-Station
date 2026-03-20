// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Bloodstream;
using Content.Server.Heretic.Components.PathSpecific;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Medical.Shared.Wounds; // Shitmed Change

namespace Content.Server.Heretic.EntitySystems.PathSpecific;

public sealed class ChampionStanceSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChampionStanceComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<ChampionStanceComponent, BeforeStaminaDamageEvent>(OnBeforeStaminaDamage);
        SubscribeLocalEvent<ChampionStanceComponent, GetBloodlossDamageMultiplierEvent>(OnGetBloodlossMultiplier);
        SubscribeLocalEvent<ChampionStanceComponent, ComponentStartup>(OnChampionStartup);
        SubscribeLocalEvent<ChampionStanceComponent, ComponentShutdown>(OnChampionShutdown);
        SubscribeLocalEvent<ChampionStanceComponent, ModifySlowOnDamageSpeedEvent>(OnChampionModifySpeed);

        // if anyone is reading through and does not have EE newmed you can remove these handlers
        SubscribeLocalEvent<ChampionStanceComponent, OrganInsertedIntoEvent>(OnOrganInsertedInto);
        SubscribeLocalEvent<ChampionStanceComponent, OrganRemovedFromEvent>(OnOrganRemovedFrom);
    }

    private void OnChampionModifySpeed(Entity<ChampionStanceComponent> ent, ref ModifySlowOnDamageSpeedEvent args)
    {
        var dif = 1f - args.Speed;
        if (dif <= 0f)
            return;

        // reduces the slowness modifier by the given coefficient
        args.Speed += dif * 0.5f;
    }

    private void OnChampionShutdown(Entity<ChampionStanceComponent> ent, ref ComponentShutdown args)
    {
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(ent);
    }

    private void OnChampionStartup(Entity<ChampionStanceComponent> ent, ref ComponentStartup args)
    {
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(ent);
    }

    private void OnGetBloodlossMultiplier(Entity<ChampionStanceComponent> ent,
        ref GetBloodlossDamageMultiplierEvent args)
    {
        args.Multiplier *= 0.5f;
    }

    public bool Condition(Entity<ChampionStanceComponent> ent)
    {
        if (!TryComp(ent, out MobThresholdsComponent? thresholdComp))
            return false;

        if (!_threshold.TryGetThresholdForState(ent, MobState.Critical, out var threshold, thresholdComp))
            threshold = _threshold.GetThresholdForState(ent, MobState.Dead, thresholdComp);
        return _damageable.GetTotalDamage(ent.Owner) >= threshold.Value / 2;
    }

    private void OnDamageModify(Entity<ChampionStanceComponent> ent, ref DamageModifyEvent args)
    {
        if (!Condition(ent))
            return;

        args.Damage = args.OriginalDamage / 2f;
    }

    private void OnBeforeStaminaDamage(Entity<ChampionStanceComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        if (!Condition(ent))
            return;

        args.Value *= 0.4f;
    }

    private void OnOrganInsertedInto(Entity<ChampionStanceComponent> ent, ref OrganInsertedIntoEvent args)
    {
        // can't touch this
        if (!TryComp(args.Organ, out WoundableComponent? woundable))
            return;

        woundable.CanRemove = false;
        Dirty(args.Organ, woundable);
    }

    private void OnOrganRemovedFrom(Entity<ChampionStanceComponent> ent, ref OrganRemovedFromEvent args)
    {
        // can touch this
        if (!TryComp(args.Organ, out WoundableComponent? woundable))
            return;

        woundable.CanRemove = true;
        Dirty(args.Organ, woundable);
    }
}
