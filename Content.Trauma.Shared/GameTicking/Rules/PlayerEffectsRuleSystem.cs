// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.GameTicking;

namespace Content.Trauma.Shared.GameTicking.Rules;

public sealed partial class PlayerEffectsRuleSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        var query = EntityQueryEnumerator<PlayerEffectsRuleComponent>();
        while (query.MoveNext(out var comp))
        {
            if (comp.Jobs is {} jobs && (args.JobId is not {} job || !jobs.Contains(job)))
                continue;

            _effects.ApplyEffects(args.Mob, comp.Effects, predicted: false);
        }
    }
}
