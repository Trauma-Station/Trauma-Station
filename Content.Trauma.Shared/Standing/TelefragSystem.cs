// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Map;

namespace Content.Trauma.Shared.Standing;

public sealed partial class TelefragSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    private HashSet<Entity<CrawlerComponent>> _targets = new();

    public void DoTelefrag(EntityUid uid,
        EntityCoordinates coords,
        TimeSpan knockdownTime,
        float range = 0.3f,
        bool drop = true,
        bool autoStand = false)
    {
        if (range <= 0f || knockdownTime <= TimeSpan.Zero)
            return;

        _targets.Clear();
        _lookup.GetEntitiesInRange(coords, range, _targets, LookupFlags.Dynamic);
        foreach (var ent in _targets)
        {
            if (ent.Owner != uid && !_standing.IsDown(ent.Owner))
                _stun.TryKnockdown(ent.AsNullable(), knockdownTime, true, autoStand: autoStand, drop: drop);
        }
    }
}
