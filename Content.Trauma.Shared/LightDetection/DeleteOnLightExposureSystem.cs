// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.LightDetection.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.LightDetection;

public sealed partial class DeleteOnLightExposureSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeleteOnLightExposureComponent, LightLevelUpdated>(OnLightUpdate);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now  = _timing.CurTime;

        // TODO: If this turns out to be bad for perf. , make active component
        var eqe = EntityQueryEnumerator<DeleteOnLightExposureComponent>();
        while (eqe.MoveNext(out var uid, out var comp))
        {
            if (!comp.Active)
                continue;

            if (now < comp.Update)
                continue;

            PredictedQueueDel(uid);
        }
    }

    private void OnLightUpdate(Entity<DeleteOnLightExposureComponent> ent, ref LightLevelUpdated args)
    {
        if (args.NewLightLevel <= ent.Comp.LightLevel)
        {
            ent.Comp.Active = false;
            ent.Comp.Update = TimeSpan.Zero;
            Dirty(ent);
            return;
        }

        if (ent.Comp.Active)
            return;

        ent.Comp.Update = _timing.CurTime + ent.Comp.Duration;
        ent.Comp.Active = true;
        Dirty(ent);
    }
}
