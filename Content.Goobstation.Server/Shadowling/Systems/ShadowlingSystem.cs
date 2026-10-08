// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Shadowling;
using Content.Goobstation.Shared.Shadowling.Components;
using Content.Goobstation.Shared.Shadowling.Systems;
using Content.Server.Objectives.Systems;

namespace Content.Goobstation.Server.Shadowling.Systems;

/// <summary>
/// This handles the Shadowling's ascending objective
/// </summary>
public sealed partial class ShadowlingSystem : SharedShadowlingSystem
{
    [Dependency] private CodeConditionSystem _codeCondition = default!;

    [SubscribeLocalEvent]
    private void OnAscend(ShadowlingAscendEvent args)
    {
        if (TryComp<ShadowlingComponent>(args.ShadowlingAscended, out var comp))
            _codeCondition.SetCompleted(args.ShadowlingAscended, comp.ObjectiveAscend);
    }
}
