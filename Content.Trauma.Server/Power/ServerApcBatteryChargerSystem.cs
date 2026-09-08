// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Power.Components;
using Content.Trauma.Shared.Power;

namespace Content.Trauma.Server.Power;

/// <summary>
/// Updates battery charge rates as the receiver ramps up and down.
/// </summary>
public sealed partial class ServerApcBatteryChargerSystem : ApcBatteryChargerSystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ApcBatteryChargerComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var uid, out var comp, out var power))
        {
            var ent = (uid, comp);
            SetChargeRate(ent, CalcChargeRate(ent, power.PowerReceived));
        }
    }
}
