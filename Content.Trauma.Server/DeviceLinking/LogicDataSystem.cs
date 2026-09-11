// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceNetwork.Events;
using Content.Trauma.Common.DeviceLinking;

namespace Content.Trauma.Server.DeviceLinking;

public sealed partial class LogicDataSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _device = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeviceLinkSinkComponent, DeviceNetworkPacketEvent<SignalPayload<LogicIntPayload>>>(_device.OnSignalReceived);
        SubscribeLocalEvent<DeviceLinkSinkComponent, DeviceNetworkPacketEvent<SignalPayload<LogicStringPayload>>>(_device.OnSignalReceived);
    }
}
