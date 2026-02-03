using Content.Trauma.Shared.DeepFryer.Components;
using Content.Trauma.Shared.DeepFryer.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.DeepFryer;

public sealed class ServerDeepFryerSystem : DeepFryerSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query1 = EntityQueryEnumerator<DeepFryerComponent>();
        while (query1.MoveNext(out var fryerUid, out var fryer))
        {
            if (fryer.StoredObjects.Count == 0)
                continue;

            AddHeatDamage(fryer, frameTime);

            if (fryer.FryFinishTime < _gameTiming.CurTime && fryer.FryFinishTime != TimeSpan.Zero)
            {
                DeepFryItems((fryerUid,fryer));
            }
        }
    }
}
