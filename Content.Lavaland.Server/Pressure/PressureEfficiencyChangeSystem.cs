// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Lavaland.Shared.Pressure;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;

namespace Content.Lavaland.Server.Pressure;

public sealed partial class PressureEfficiencyChangeSystem : SharedPressureEfficiencyChangeSystem
{
    [Dependency] private AtmosphereSystem _atmos = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PressureTrackerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PressureTrackerComponent, AtmosExposedUpdateEvent>(OnAtmosExposedUpdate);
    }

    private void OnMapInit(Entity<PressureTrackerComponent> ent, ref MapInitEvent args)
    {
        EnsureComp<AtmosExposedComponent>(ent); // should already be the case for mobs, but not necessarily for PKA turrets and stuff
        // initialize immediately instead of waiting for next atmos tick (up to 0.5s away)
        ent.Comp.Pressure = _atmos.GetTileMixture((ent.Owner, Transform(ent)))?.Pressure ?? 0f;
        Dirty(ent);
    }

    private void OnAtmosExposedUpdate(Entity<PressureTrackerComponent> ent, ref AtmosExposedUpdateEvent args)
    {
        var pressure = args.GasMixture.Pressure;
        if (ent.Comp.Pressure == pressure)
            return;

        ent.Comp.Pressure = pressure;
        Dirty(ent);
    }
}
