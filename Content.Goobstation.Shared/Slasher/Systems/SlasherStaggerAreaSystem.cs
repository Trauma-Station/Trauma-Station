// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Slasher.Components;
using Content.Goobstation.Shared.Slasher.Events;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Content.Shared.StatusEffectNew;

namespace Content.Goobstation.Shared.Slasher.Systems;

/// <summary>
/// Handles the Slasher Stagger Area action. When used, slows nearby mobs in range for a short duration.
/// </summary>
public sealed partial class SlasherStaggerAreaSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedInteractionSystem _interact = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private INetManager _net = default!;

    private static readonly EntProtoId StaggerEffect = "SlasherStaggerStatusEffect";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlasherStaggerAreaComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SlasherStaggerAreaComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SlasherStaggerAreaComponent, SlasherStaggerAreaEvent>(OnUse);
    }

    private void OnMapInit(Entity<SlasherStaggerAreaComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEnt, ent.Comp.ActionId);
    }

    private void OnShutdown(Entity<SlasherStaggerAreaComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Comp.ActionEnt);
    }

    private void OnUse(Entity<SlasherStaggerAreaComponent> ent, ref SlasherStaggerAreaEvent args)
    {
        if (args.Handled)
            return;

        var (uid, comp) = ent;

        foreach (var (targetUid, _) in _lookup.GetEntitiesInRange<BodyComponent>(Transform(uid).Coordinates, comp.Range, LookupFlags.Dynamic))
        {
            if (targetUid == uid)
                continue;

            if (!_interact.InRangeUnobstructed(uid, targetUid, comp.Range))
                continue;

            _status.TryUpdateStatusEffectDuration(targetUid, StaggerEffect, comp.SlowDuration);

            // Show popup to the victim
            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString("slasher-staggerarea-victim"), targetUid, targetUid, PopupType.MediumCaution);
        }

        _audio.PlayPredicted(comp.StaggerSound, uid, uid);

        // Show popup to the user
        if (_net.IsServer)
            _popup.PopupEntity(Loc.GetString("slasher-staggerarea-popup"), uid, uid, PopupType.MediumCaution);

        args.Handled = true;
    }
}
