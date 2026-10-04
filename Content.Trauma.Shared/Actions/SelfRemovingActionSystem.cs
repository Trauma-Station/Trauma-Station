// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Charges.Systems;

namespace Content.Trauma.Shared.Actions;

public sealed partial class SelfRemovingActionSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedChargesSystem _charges = default!;

    [SubscribeLocalEvent(after: [typeof(SharedChargesSystem)])]
    private void OnPerformed(Entity<SelfRemovingActionComponent> ent, ref ActionPerformedEvent args)
    {
        if (_charges.IsEmpty(ent.Owner))
            _actions.RemoveAction(ent.Owner);
    }
}
