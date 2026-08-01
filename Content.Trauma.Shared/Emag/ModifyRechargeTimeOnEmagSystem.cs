// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Emag.Systems;
using Content.Shared.Charges.Components;

namespace Content.Trauma.Shared.Emag;

public sealed partial class EmagEmagSystem : EntitySystem
{
    [Dependency] private EmagSystem _emag = default!;

    [SubscribeLocalEvent]
    private void OnEmag(Entity<ModifyRechargeTimeOnEmagComponent> ent, ref GotEmaggedEvent args)
    {
        if (!TryComp(ent, out AutoRechargeComponent? autoRecharge))
            return;

        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        args.Handled = true;

        autoRecharge.RechargeDuration *= ent.Comp.Multiplier;
        Dirty(ent, autoRecharge);
    }
}
