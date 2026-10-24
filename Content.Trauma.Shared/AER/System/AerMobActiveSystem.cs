// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Trauma.Shared.AER;

public sealed partial class AerMobActiveSystem : EntitySystem
{
    /// <summary>
    /// handling the aer active state on map init
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<AerMobActiveComponent> aerMob, ref MapInitEvent args)
    {
        if (TryComp<MobStateComponent>(aerMob.Owner, out var mobComponent))
        {
            bool active = mobComponent.CurrentState == MobState.Alive;

            var activeEvent = new AerUpdateActiveStatusEvent(aerMob.Owner, active);
            RaiseLocalEvent(aerMob.Owner, ref activeEvent);
        }
    }

    /// <summary>
    /// handling of the aer active status for mobs it determines if aer is healty enough to produce rd points
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<AerMobActiveComponent> ent, ref MobStateChangedEvent args)
    {
        bool active = args.NewMobState == MobState.Alive;

        var activeEvent = new AerUpdateActiveStatusEvent(ent.Owner, active);
        RaiseLocalEvent(ent.Owner, ref activeEvent);
    }
}
