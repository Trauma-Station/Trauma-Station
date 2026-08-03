// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Atmos;
using Content.Shared.Temperature;

namespace Content.Trauma.Shared.Temperature;

public sealed partial class TemperatureImmunitySystem : EntitySystem
{
    // it was already hardcoded so idc
    private const float IdealTemperature = Atmospherics.T0C + 37f;

    [SubscribeLocalEvent]
    private void OnCheckLowTemperatureImmunity(Entity<SpecialLowTempImmunityComponent> ent, ref BeforeHeatExchangeEvent args)
    {
        if (args.OurTemp < IdealTemperature)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnCheckHighTemperatureImmunity(Entity<SpecialHighTempImmunityComponent> ent, ref BeforeHeatExchangeEvent args)
    {
        if (args.OurTemp > IdealTemperature)
            args.Cancelled = true;
    }
}
