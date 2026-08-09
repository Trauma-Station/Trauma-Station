// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Wraith.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.UserInterface;
using Content.Trauma.Common.RadialSelector;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Wraith.Systems;

/// <summary>
/// Handles UI opening of spook menu and activating an action.
/// The actions exist in the server-sided system.
/// </summary>
public sealed partial class SpookSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private IGameTiming _timing = default!;

    private CompName _actionName;

    public override void Initialize()
    {
        base.Initialize();

        _actionName = Factory.CompName<ActionComponent>();
    }

    #region UI
    [SubscribeLocalEvent]
    private void OnUIOpenAttempt(Entity<SpookComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (!HasComp<WraithComponent>(args.User))
            args.Cancel();

        _ui.SetUiState(ent.Owner,
            RadialSelectorUiKey.Key,
            new RadialSelectorState(ent.Comp.Actions));
    }

    [SubscribeLocalEvent]
    private void OnRadialSelectorSelected(Entity<SpookComponent> ent, ref RadialSelectorSelectedMessage args)
    {
        DoSelectedAction(ent.Owner, args.SelectedItem);

        _ui.CloseUi(ent.Owner, RadialSelectorUiKey.Key);
    }
    #endregion
    #region Helpers
    private void DoSelectedAction(EntityUid uid, string? action)
    {
        if (action == null
            || !ProtoMan.TryIndex(action, out var actionProto)
            || !actionProto.HasComp(_actionName)
            || !TryComp<ActionsComponent>(uid, out var actions))
            return;

        foreach (var actionEnt in actions.Actions)
        {
            if (Prototype(actionEnt) != actionProto
                || !TryComp<ActionComponent>(actionEnt, out var actionComp)
                || _actions.IsCooldownActive(actionComp, _timing.CurTime))
                continue;

            _actions.PerformAction(uid, (actionEnt, actionComp));
            break;
        }
    }

    #endregion
}
