// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Botany.Events;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Popups;

namespace Content.Trauma.Shared.Botany;

public sealed partial class PlantHarvestLimitSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnHarvestAttempt(Entity<PlantHarvestLimitComponent> ent, ref PlantHarvestAttemptEvent args)
    {
        if (Count<ProduceComponent>() <= ent.Comp.Limit)
            return; // halal

        // haram
        _popup.PopupEntity("There are too many plants!", ent, args.User);
        args.Cancelled = true;
    }
}
