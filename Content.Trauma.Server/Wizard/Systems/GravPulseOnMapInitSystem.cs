// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Singularity.EntitySystems;
using Content.Trauma.Server.Wizard.Components;

namespace Content.Trauma.Server.Wizard.Systems;

public sealed class GravPulseOnMapInitSystem : EntitySystem
{
    [Dependency] private readonly GravityWellSystem _gravityWell = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GravPulseOnMapInitComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<GravPulseOnMapInitComponent> ent, ref MapInitEvent args)
    {
        var (uid, comp) = ent;

        _gravityWell.GravPulse(uid,
            comp.MaxRange,
            comp.MinRange,
            comp.BaseRadialAcceleration,
            comp.BaseTangentialAcceleration);
    }
}
