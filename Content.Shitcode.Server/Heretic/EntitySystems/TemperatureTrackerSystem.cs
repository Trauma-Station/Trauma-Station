using Content.Server.Atmos.EntitySystems;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared.Temperature;
using Robust.Shared.Timing;

namespace Content.Server._Shitcode.Heretic.EntitySystems;

public sealed class TemperatureTrackerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TemperatureTrackerComponent, AtmosExposedUpdateEvent>(OnAtmosExposed);
    }

    private void OnAtmosExposed(Entity<TemperatureTrackerComponent> ent, ref AtmosExposedUpdateEvent args)
    {
        if (ent.Comp.NextUpdate > _timing.CurTime)
            return;

        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateDelay;

        var temp = args.GasMixture.Temperature;
        if (MathHelper.CloseToPercent(temp, ent.Comp.Temperature))
            return;

        ent.Comp.Temperature = temp;
        Dirty(ent);
    }
}
