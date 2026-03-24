using Content.Server.Atmos.EntitySystems;
using Content.Trauma.Shared.Heretic.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Heretic.Systems;

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
