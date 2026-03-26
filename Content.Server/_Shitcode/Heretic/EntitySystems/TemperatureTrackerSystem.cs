using Content.Server.Atmos.EntitySystems;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared.Atmos;
using Content.Shared.Temperature;
using Robust.Shared.Timing;

namespace Content.Server._Shitcode.Heretic.EntitySystems;

public sealed class TemperatureTrackerSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TemperatureTrackerComponent, AtmosExposedUpdateEvent>(OnAtmosExposed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // set them to CMB while in space, AtmosExposedUpdateEvent doesn't get raised if you are in space
        var query = EntityQueryEnumerator<TemperatureTrackerComponent>();
        var now = _timing.CurTime;
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextUpdate)
                continue;

            comp.NextUpdate = now + comp.UpdateDelay;
            var temp = _atmos.GetContainingMixture(uid)?.Temperature ?? Atmospherics.TCMB;
            SetTemp((uid, comp), temp);
        }
    }

    private void OnAtmosExposed(Entity<TemperatureTrackerComponent> ent, ref AtmosExposedUpdateEvent args)
    {
        if (ent.Comp.NextUpdate > _timing.CurTime)
            return;

        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateDelay;

        var temp = args.GasMixture.Temperature;
        SetTemp(ent, temp);
    }

    private void SetTemp(Entity<TemperatureTrackerComponent> ent, float temp)
    {
        if (MathHelper.CloseToPercent(temp, ent.Comp.Temperature))
            return;

        ent.Comp.Temperature = temp;
        Dirty(ent);
    }
}
