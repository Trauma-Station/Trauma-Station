// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking;
using Content.Shared.DeviceNetwork;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Radio;

namespace Content.Trauma.Shared.Radio;

public sealed partial class SignalRadioReceiverSystem : EntitySystem
{
    [Dependency] private SharedDeviceLinkSystem _device = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;

    [SubscribeLocalEvent]
    private void OnRadioReceive(Entity<SignalRadioReceiverComponent> ent, ref RadioReceiveEvent args)
    {
        if (ent.Owner == args.RadioSource || !_power.IsPowered(ent.Owner))
            return;

        var data = new NetworkPayload()
        {
            // language is ignored unlucky
            ["logic_string"] = args.OriginalChatMsg.Message
        };
        _device.InvokePort(ent.Owner, ent.Comp.Port, data);
    }
}
