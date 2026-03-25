// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Components.PathSpecific.Rust;
using Content.Trauma.Shared.Heretic.Systems.Abilities;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed class RustbringerSystem : EntitySystem
{
    [Dependency] private readonly SharedHereticAbilitySystem _ability = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var rustBringerQuery = EntityQueryEnumerator<RustbringerComponent, TransformComponent>();
        while (rustBringerQuery.MoveNext(out var rustBringer, out var xform))
        {
            if (rustBringer.NextUpdate > now)
                continue;

            rustBringer.NextUpdate = now + rustBringer.Delay;

            if (!_ability.IsTileRust(xform.Coordinates, out _))
                continue;

            Spawn(rustBringer.Effect, xform.Coordinates);
        }
    }
}
