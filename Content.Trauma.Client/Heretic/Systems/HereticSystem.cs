// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Systems;

namespace Content.Trauma.Client.Heretic.Systems;

public sealed class HereticSystem : SharedHereticSystem
{
    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Timing.IsFirstTimePredicted)
            return;

        var now = Timing.CurTime;

        var query = EntityQueryEnumerator<HereticSacrificeTargetComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.RemovalTimer)
                continue;

            RemCompDeferred(uid, comp);
        }
    }
}
