// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Server.Heretic.Abilities;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Rust;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed class RustObjectsInRadiusSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly HereticAbilitySystem _ability = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var rustChargeQuery = EntityQueryEnumerator<RustObjectsInRadiusComponent, TransformComponent>();
        while (rustChargeQuery.MoveNext(out var uid, out var rust, out var xform))
        {
            if (rust.NextRustTime > now)
                continue;

            rust.NextRustTime = now + rust.RustPeriod;
            _ability.RustObjectsInRadius(_transform.GetMapCoordinates(uid, xform),
                rust.RustRadius,
                rust.TileRune,
                rust.LookupRange,
                rust.RustStrength);
        }
    }
}
