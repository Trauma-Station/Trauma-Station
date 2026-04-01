// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Atmos.Components;
using Content.Shared.Mobs;
using Content.Trauma.Server.Heretic.Components.PathSpecific;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Events;
using Robust.Server.GameObjects;

namespace Content.Trauma.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem
{
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly TransformSystem _xform = default!;

    protected override void SubscribeAsh()
    {
        base.SubscribeAsh();

        SubscribeLocalEvent<EventHereticAshenShift>(OnJaunt);
        SubscribeLocalEvent<EventHereticNightwatcherRebirth>(OnNWRebirth);
        SubscribeLocalEvent<EventHereticFlames>(OnFlames);
        SubscribeLocalEvent<EventHereticCascade>(OnCascade);

        SubscribeLocalEvent<Shared.Heretic.Components.PathSpecific.Ash.NightwatcherRebirthActionComponent, ActionPerformedEvent>(OnRebirthPerformed);
    }

    private void OnRebirthPerformed(Entity<Shared.Heretic.Components.PathSpecific.Ash.NightwatcherRebirthActionComponent> ent, ref ActionPerformedEvent args)
    {
        if (ent.Comp.LastTargets == 0 || !TryComp(ent, out ActionComponent? action) || action.Cooldown is not { } cd)
            return;

        var total = cd.End - cd.Start;
        if (total <= ent.Comp.MinCooldown)
            return;

        var newCd = total - ent.Comp.LastTargets * ent.Comp.CooldownReductionPerVictim;
        if (newCd < ent.Comp.MinCooldown)
            newCd = ent.Comp.MinCooldown;

        _actions.SetCooldown((ent, action), newCd);
        ent.Comp.LastTargets = 0;
    }

    private void OnJaunt(EventHereticAshenShift args)
    {
        if (!TryUseAbility(args))
            return;

        Spawn("PolymorphAshJauntAnimation", Transform(args.Performer).Coordinates);
        _poly.PolymorphEntity(args.Performer, args.Jaunt);
    }

    private void OnNWRebirth(EventHereticNightwatcherRebirth args)
    {
        if (!TryComp(args.Action, out Shared.Heretic.Components.PathSpecific.Ash.NightwatcherRebirthActionComponent? nwAction))
            return;

        nwAction.LastTargets = 0;

        if (!TryUseAbility(args))
            return;

        Heretic.TryGetHereticComponent(args.Performer, out var heretic, out _);

        if (heretic is not { Ascended: true, CurrentPath: HereticPath.Ash })
            _flammable.Extinguish(args.Performer);

        var lookup = GetNearbyPeople(args.Performer, args.Range, heretic?.CurrentPath ?? HereticPath.Ash);
        var toHeal = 0f;

        var flamQuery = GetEntityQuery<FlammableComponent>();
        foreach (var (look, mobstate) in lookup)
        {
            if (mobstate.CurrentState == MobState.Dead)
                continue;

            if (!flamQuery.TryComp(look, out var flam) || !flam.OnFire)
                continue;

            if (mobstate.CurrentState == MobState.Critical)
                _mobstate.ChangeMobState(look, MobState.Dead, mobstate);

            toHeal += args.HealAmount;
            nwAction.LastTargets++;

            _flammable.Extinguish(look, flam);
            _dmg.ChangeDamage(look,
                args.Damage * _body.GetVitalBodyPartRatio(look),
                true,
                targetPart: TargetBodyPart.All,
                splitDamage: SplitDamageBehavior.SplitEnsureAll);
        }

        var coords = _transform.GetMapCoordinates(args.Performer);
        var effect = Spawn(args.Effect, coords);
        if (TryComp(effect, out Shared.Heretic.Components.Side.AreaGraspEffectComponent? grasp))
        {
            grasp.SpawnTime = Timing.CurTime;
            Dirty(effect, grasp);
        }

        if (toHeal >= 0)
            return;

        _stam.TryTakeStamina(args.Performer, toHeal);
        IHateWoundMed(args.Performer, AllDamage * toHeal, 0, 0);
    }

    private void OnFlames(EventHereticFlames args)
    {
        if (!TryUseAbility(args))
            return;

        EnsureComp<HereticFlamesComponent>(args.Performer);
    }

    private void OnCascade(EventHereticCascade args)
    {
        if (!Transform(args.Performer).GridUid.HasValue || !TryUseAbility(args))
            return;

        Spawn(args.CascadeEnt, _xform.GetMapCoordinates(args.Performer));
    }
}
