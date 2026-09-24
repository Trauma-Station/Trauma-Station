// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Actions.Events;

namespace Content.Trauma.Shared.Actions;

public sealed partial class SelfRemovingActionSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;

    [SubscribeLocalEvent]
    private void OnPerformed(Entity<SelfRemovingActionComponent> ent, ref ActionPerformedEvent args)
    {
        _actions.RemoveAction(ent.Owner);
    }
}
