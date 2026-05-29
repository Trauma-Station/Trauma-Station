// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Alert;
using Content.Shared.Alert.Components;
using Content.Trauma.Shared.Vampires;

namespace Content.Trauma.Client.Vampires;

/// <summary>
/// Handles updating the generic counter for the vampire blood level alert.
/// </summary>
public sealed class VampireAlertSystem : EntitySystem
{
    private static readonly ProtoId<AlertPrototype> BloodLevelAlert = "BloodLevel";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireComponent, GetGenericAlertCounterAmountEvent>(OnAlertCounter);
    }

    private void OnAlertCounter(Entity<VampireComponent> ent, ref GetGenericAlertCounterAmountEvent args)
    {
        if (args.Handled || args.Alert != BloodLevelAlert)
            return;

        args.Amount = ent.Comp.TotalBlood;
    }
}
