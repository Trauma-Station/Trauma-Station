// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Destructible;
using Content.Shared.Destructible;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Rust;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Rust;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed class RustChargeSystem : SharedRustChargeSystem
{
    [Dependency] private readonly DestructibleSystem _destructible = default!;

    protected override void DestroyStructure(EntityUid uid, EntityUid user)
    {
        base.DestroyStructure(uid, user);

        if (TryComp(uid, out RustRequiresPathStageComponent? rusty) && rusty.PathStage > 10)
            return;

        if (!TryComp(uid, out DestructibleComponent? destructible) || destructible.Thresholds.Count == 0)
        {
            Del(uid);
            return;
        }

        var threshold = destructible.Thresholds[^1];
        RaiseLocalEvent(uid, new DamageThresholdReached(destructible, threshold), true);
        _destructible.Execute(threshold, uid, user);
    }
}
