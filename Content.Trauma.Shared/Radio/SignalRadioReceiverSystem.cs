// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Radio;
using Content.Trauma.Common.DeviceLinking;

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

        // language is ignored unlucky
        var payload = new LogicStringPayload(args.OriginalChatMsg.Message);
        _device.InvokePort(ent.Owner, ent.Comp.Port, ref payload);
    }
}
