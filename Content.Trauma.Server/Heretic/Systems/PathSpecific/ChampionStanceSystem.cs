// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Trauma.Server.Heretic.Components.PathSpecific;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed partial class ChampionStanceSystem : EntitySystem
{
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private BodySystem _body = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChampionStanceComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<ChampionStanceComponent, ComponentStartup>(OnChampionStartup);
        SubscribeLocalEvent<ChampionStanceComponent, ComponentShutdown>(OnChampionShutdown);
        SubscribeLocalEvent<ChampionStanceComponent, OrganInsertedIntoEvent>(OnOrganInsertedInto);
        SubscribeLocalEvent<ChampionStanceComponent, OrganRemovedFromEvent>(OnOrganRemovedFrom);
    }

    private void OnChampionShutdown(Entity<ChampionStanceComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        MakeOrgansRemovable(ent, true);
        _movement.RefreshMovementSpeedModifiers(ent);
    }

    private void OnChampionStartup(Entity<ChampionStanceComponent> ent, ref ComponentStartup args)
    {
        MakeOrgansRemovable(ent, false);
        _movement.RefreshMovementSpeedModifiers(ent);
    }

    private void MakeOrgansRemovable(EntityUid uid, bool removable)
    {
        foreach (var part in _body.GetOrgans<WoundableComponent>(uid))
        {
            part.Comp.CanRemove = removable;
            Dirty(part);
        }
    }

    public bool Condition(Entity<ChampionStanceComponent> ent)
    {
        if (!TryComp(ent, out MobThresholdsComponent? thresholdComp))
            return false;

        if (!_threshold.TryGetThresholdForState(ent, MobState.SoftCrit, out var threshold, thresholdComp) &&
            !_threshold.TryGetThresholdForState(ent, MobState.Critical, out threshold, thresholdComp) &&
            !_threshold.TryGetThresholdForState(ent, MobState.Dead, out threshold, thresholdComp))
            return false;

        return _threshold.CheckVitalDamage(ent.Owner) >= threshold.Value * 0.5f;
    }

    private void OnDamageModify(Entity<ChampionStanceComponent> ent, ref DamageModifyEvent args)
    {
        if (!Condition(ent))
            return;

        var dict = args.OriginalDamage.DamageDict.ToDictionary();
        foreach (var key in dict.Keys)
        {
            if (args.OriginalDamage.WoundSeverityMultipliers.TryGetValue(key, out var existing))
                dict[key] = existing * 0.5f;
            else
                dict[key] = 0.5f;
        }

        args.Damage.WoundSeverityMultipliers = dict;
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
