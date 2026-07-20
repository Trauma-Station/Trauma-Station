// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Alert.Components;
using Content.Trauma.Shared.Morph;

namespace Content.Trauma.Client.Morph;

/// <summary>
/// Handles setting the morph's biomass alert UI number
/// </summary>
public sealed class MorphAlertSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MorphComponent, GetGenericAlertCounterAmountEvent>(OnUpdateAlert);
    }

    private void OnUpdateAlert(Entity<MorphComponent> ent, ref GetGenericAlertCounterAmountEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.BiomassAlert != args.Alert)
            return;

        args.Amount = ent.Comp.Biomass.Int();
    }
}
