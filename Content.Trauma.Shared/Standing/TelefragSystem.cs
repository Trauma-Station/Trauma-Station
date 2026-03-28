// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Map;

namespace Content.Trauma.Shared.Standing;

public sealed class TelefragSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public void DoTelefrag(EntityUid uid,
        EntityCoordinates coords,
        TimeSpan knockdownTime,
        float range = 0.3f,
        bool drop = true,
        bool autoStand = false)
    {
        if (range <= 0f || knockdownTime <= TimeSpan.Zero)
            return;

        var entities = _lookup.GetEntitiesInRange(coords, range, LookupFlags.Dynamic);
        foreach (var ent in entities.Where(ent => ent != uid && !_standing.IsDown(ent)))
        {
            _stun.TryKnockdown(ent, knockdownTime, true, autoStand: autoStand, drop: drop);
        }
    }
}
