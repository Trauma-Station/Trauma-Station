// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 Spatison <137375981+Spatison@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Map;

namespace Content.Shared._White.Standing;

public sealed class TelefragSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

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
