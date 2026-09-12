// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Forensics.Systems;
using Content.Trauma.Shared.Forensics;
using Content.Shared.Forensics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Client.Player;

namespace Content.Trauma.Client.Forensics;

public sealed partial class ScentTrackerSystem : SharedScentTrackerSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IPlayerManager _player = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // If the player can't track scents, continuing beyond this point is a waste of processing power.
        if (!TryComp<ScentTrackerComponent>(_player.LocalEntity, out var scentcomp) || scentcomp.Scent == string.Empty)
            return;

        var now = _timing.CurTime;
        if (now < scentcomp.NextEffects)
            return;

        scentcomp.NextEffects = now + TimeSpan.FromSeconds(1);

        var query = AllEntityQuery<ForensicsComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (scentcomp.Scent == comp.Scent)
            {
                Spawn("ScentTrackEffect", _transform.GetMapCoordinates(uid).Offset(_random.NextVector2(0.25f)));
            }
        }
    }
}
