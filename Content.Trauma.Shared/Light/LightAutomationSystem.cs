// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Trauma.Common.DeviceLinking;

namespace Content.Trauma.Shared.Light;

public sealed partial class LightAutomationSystem : EntitySystem
{
    [Dependency] private SharedPoweredLightSystem _light = default!;

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<PoweredLightComponent> ent, ref SignalReceivedEvent<LogicStatePayload> args)
    {
        if (args.Port == ent.Comp.ControlPort)
            _light.SetState(ent.Owner, args.Data.State != SignalState.Low, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnSignalIntReceived(Entity<PoweredLightComponent> ent, ref SignalReceivedEvent<LogicIntPayload> args)
    {
        if (args.Port == ent.Comp.ControlPort)
            _light.SetState(ent.Owner, args.Data.Value != 0, ent.Comp);
    }
}
