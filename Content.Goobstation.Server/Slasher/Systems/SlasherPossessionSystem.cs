// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.Possession;
using Content.Goobstation.Shared.Possession;
using Content.Goobstation.Shared.Slasher.Components;
using Content.Goobstation.Shared.Slasher.Events;
using Content.Shared.Actions;
using Content.Shared.Mindshield;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;

namespace Content.Goobstation.Server.Slasher.Systems;

public sealed partial class SlasherPossessionSystem : EntitySystem
{
    [Dependency] private MindShieldSystem _mindShield = default!;
    [Dependency] private PossessionSystem _possession = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<SlasherPossessionComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEnt, ent.Comp.ActionId);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<SlasherPossessionComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEnt);
    }

    /// <summary>
    /// Slasher - Handles the possession of a target.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnPossess(Entity<SlasherPossessionComponent> ent, ref SlasherPossessionEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<MobStateComponent>(args.Target))
            return;

        // Check if the target has a mindshield and return early
        if (ent.Comp.DoesMindshieldBlock && _mindShield.IsShielded(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("possession-fail-target-shielded"), ent.Owner, ent.Owner);
            return;
        }

        // Posses Target
        var ok = _possession.TryPossessTarget(args.Target,
            ent.Owner,
            ent.Comp.PossessionDuration,
            pacifyPossessed: false,
            hideActions: false, // Doesn't actually work I guess
            polymorphPossessor: true);

        // Ensure our actions are not hidden when we posses our target
        if (TryComp<PossessedComponent>(args.Target, out var possessed))
            _actions.UnHideActions(args.Target, possessed.HiddenActions); // required

        args.Handled = true;
    }
}
