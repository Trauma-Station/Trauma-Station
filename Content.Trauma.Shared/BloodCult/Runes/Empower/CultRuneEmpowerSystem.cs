// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.BloodCult.Empower;

namespace Content.Trauma.Shared.BloodCult.Runes.Empower;

public sealed partial class CultRuneEmpowerSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnRuneInvoke(Entity<CultRuneEmpowerComponent> ent, ref RuneInvokeEvent args)
    {
        // TODO: make it use compregistry to be more generic
        var user = args.User;
        if (HasComp<BloodCultEmpoweredComponent>(user))
        {
            args.Popup = "You are already empowered.";
            return;
        }

        EnsureComp<BloodCultEmpoweredComponent>(user);
        args.Handled = true;
    }
}
