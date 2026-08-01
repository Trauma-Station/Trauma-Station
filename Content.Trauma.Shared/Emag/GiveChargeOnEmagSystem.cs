// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Emag.Systems;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;

namespace Content.Trauma.Shared.Emag;

public sealed partial class EmagEmagSystem : EntitySystem
{
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private EmagSystem _emag = default!;

    [SubscribeLocalEvent]
    private void OnEmag(Entity<GiveChargeOnEmagComponent> ent, ref GotEmaggedEvent args)
    {
        if (!TryComp(ent, out LimitedChargesComponent? charges))
            return;

        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        args.Handled = true;

        var toCharge = ent.Comp.Charges;

        if (ent.Comp.RaiseMaxCharge)
        {
            var difference = charges.MaxCharges - charges.LastCharges;
            if (difference < toCharge)
                _charges.SetMaxCharges((ent, charges), charges.MaxCharges + toCharge - difference);
        }

        _charges.AddCharges((ent, charges), toCharge);
    }
}
