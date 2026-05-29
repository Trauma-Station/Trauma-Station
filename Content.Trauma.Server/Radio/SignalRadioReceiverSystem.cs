// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.DeviceLinking.Systems;
using Content.Server.Radio;
using Content.Shared.DeviceNetwork;
using Content.Shared.Power.EntitySystems;
using Content.Trauma.Shared.Radio;

namespace Content.Trauma.Server.Radio;

public sealed partial class SignalRadioReceiverSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _device = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SignalRadioReceiverComponent, RadioReceiveEvent>(OnRadioReceive);
    }

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
