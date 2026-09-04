// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Timing;

namespace Content.Trauma.Client.Spawners;

public sealed partial class TimedClientDespawnSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<TimedClientDespawnComponent>();
        foreach (var ent in query)
        {
            if (now < ent.Comp.NextDespawn)
                continue;

            if (IsClientSide(ent))
                QueueDel(ent);
            else
                RemCompDeferred(ent, ent.Comp); // bad chuddy
        }
    }

    [SubscribeLocalEvent]
    private void OnInit(Entity<TimedClientDespawnComponent> ent, ref ComponentInit args)
    {
        ent.Comp.NextDespawn = _timing.CurTime + ent.Comp.Lifetime;
    }
}
