// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Factory.Shared.Filters;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Unary.Components;

namespace Content.Factory.Server.Filters;

public sealed partial class PressureFilterSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnPressureFilter(Entity<PressureFilterComponent> ent, ref AutomationFilterEvent args)
    {
        // TODO: replace this shit with InternalAir if it gets refactored
        float pressure = 0f;
        if (TryComp<GasTankComponent>(args.Item, out var tank))
            pressure = tank.Air.Pressure;
        else if (TryComp<GasCanisterComponent>(args.Item, out var can))
            pressure = can.Air.Pressure;
        else
            return; // has to be a gas holder

        args.Allowed = pressure >= ent.Comp.Min && pressure <= ent.Comp.Max;
        args.CouldAllow = true; // pressure can change with a gas canister or if the tank/can valve is opened
    }
}
