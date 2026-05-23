// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Xenomorphs;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;

namespace Content.Trauma.Shared.Actions;

public sealed partial class ActionRelaySystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ActionsComponent, PlasmaAmountChangeEvent>(RelayEvent);
    }

    public void RelayEvent<T>(EntityUid uid, ActionsComponent component, ref T args) where T : notnull
    {
        var actions = _actions.GetActions(uid, component);
        foreach (var action in actions)
        {
            RaiseLocalEvent(action.Owner, ref args);
        }
    }
}
