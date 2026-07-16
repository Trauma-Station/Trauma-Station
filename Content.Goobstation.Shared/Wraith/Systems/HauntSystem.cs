// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Wraith.Components;
using Content.Goobstation.Shared.Wraith.Events;
using Content.Goobstation.Shared.Wraith.WraithPoints;
using Content.Shared.Interaction;
using Robust.Shared.Timing;
using Content.Shared.Actions;
using Content.Shared.Flash;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Revenant.Components;
using Content.Shared.StatusEffect;

namespace Content.Goobstation.Shared.Wraith.Systems;

//Partially ported from Impstation
public sealed partial class HauntSystem : EntitySystem
{
    [Dependency] private SharedInteractionSystem _interact = default!;
    [Dependency] private Content.Shared.StatusEffectNew.StatusEffectsSystem _status = default!;
    [Dependency] private StatusEffectsSystem _statusEffectsOld = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private WraithPointsSystem _points = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private EntityQuery<HauntedComponent> _hauntQuery = default!;
    [Dependency] private EntityQuery<WraithAbsorbableComponent> _wraithAbsorbableQuery = default!;

    private readonly HashSet<Entity<HumanoidProfileComponent>> _viewers = new();
    private readonly HashSet<Entity<StatusEffectsComponent>> _targets = new();

    private static readonly ProtoId<StatusEffectPrototype> CorporealEffect = "Corporeal";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<HauntComponent>();
        while (query.MoveNext(out var uid, out var haunt))
        {
            if (now >= haunt.NextHauntWpRegenUpdate && haunt.WpBoostActive)
            {
                // reset
                _points.SetWpRate(haunt.OriginalWpRegen, uid);
                haunt.WpBoostActive = false;
                Dirty(uid, haunt);
            }

            if (!haunt.Active)
                continue;

            if (now >= haunt.NextHauntUpdate)
            {
                _statusEffectsOld.TryRemoveStatusEffect(uid, CorporealEffect);
                haunt.Active = false;
                _actions.StartUseDelay(haunt.ActionEnt);

                Dirty(uid, haunt);
            }

            // constantly check for witnesses
            if (now >= haunt.WitnessNextUpdate)
            {
                _viewers.Clear();
                _lookup.GetEntitiesInRange(Transform(uid).Coordinates, 10f, _viewers);
                foreach (var entity in _viewers)
                {
                    // skip if we are already haunted, or if we cant be haunted
                    if (_hauntQuery.HasComp(entity) || !_wraithAbsorbableQuery.HasComp(entity) || _mobState.IsDead(entity))
                        continue;

                    if (!_interact.InRangeUnobstructed(uid, entity.Owner, 10f))
                        continue;

                    // TODO: check vision cone too
                    EnsureComp<HauntedComponent>(entity);
                    _points.AdjustWpGenerationRate(haunt.HauntWpRegenPerWitness, uid);
                }

                haunt.WitnessNextUpdate = now + haunt.WitnessUpdate;
                Dirty(uid, haunt);
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnHaunt(Entity<HauntComponent> ent, ref HauntEvent args)
    {
        if (ent.Comp.Active)
        {
            _statusEffectsOld.TryRemoveStatusEffect(ent.Owner, CorporealEffect);
            _points.SetWpRate(ent.Comp.OriginalWpRegen, ent.Owner);
            ent.Comp.Active = false;
            ent.Comp.WpBoostActive = false;
            args.Handled = true;
            Dirty(ent);

            return;
        }

        _popup.PopupClient(Loc.GetString("wraith-haunt-show"), ent.Owner, ent.Owner, PopupType.MediumCaution);
        // flash people nearby

        _targets.Clear();
        _lookup.GetEntitiesInRange(Transform(ent.Owner).Coordinates, 3f, _targets);
        foreach (var entity in _targets)
        {
            _status.TryUpdateStatusEffectDuration(entity, SharedFlashSystem.FlashedKey, ent.Comp.HauntFlashDuration);
        }

        // we don't have corporeal so add it
        _statusEffectsOld.TryAddStatusEffect<CorporealComponent>(ent.Owner, CorporealEffect, ent.Comp.HauntCorporealDuration, true);

        // set original rate for resetting it after boost
        ent.Comp.OriginalWpRegen = _points.GetCurrentWpRate(ent.Owner);

        var now = _timing.CurTime;
        // activate the haunt timer and start tracking people
        ent.Comp.Active = true;
        ent.Comp.NextHauntUpdate = now + ent.Comp.HauntDuration;
        ent.Comp.WitnessNextUpdate = now + ent.Comp.WitnessUpdate;

        // boost wp regen per witness
        ent.Comp.NextHauntWpRegenUpdate = now + ent.Comp.HauntWpRegenDuration;
        ent.Comp.WpBoostActive = true;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<HauntComponent> ent, ref MapInitEvent args) =>
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEnt, ent.Comp.ActionId);

    [SubscribeLocalEvent]
    private void OnComponentShutdown(Entity<HauntComponent> ent, ref ComponentShutdown args) =>
        _actions.RemoveAction(ent.Comp.ActionEnt);
}
