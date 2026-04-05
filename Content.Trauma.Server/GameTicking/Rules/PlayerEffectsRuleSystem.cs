// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.GameTicking;
using Content.Trauma.Server.GameTicking.Rules.Components;

namespace Content.Trauma.Server.GameTicking.Rules;

public sealed class PlayerEffectsRuleSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        var query = EntityQueryEnumerator<PlayerEffectsRuleComponent>();
        while (query.MoveNext(out var comp))
        {
            if (comp.Jobs is {} jobs && (args.JobId is not {} job || !jobs.Contains(job)))
                continue;

            _effects.ApplyEffects(args.Mob, comp.Effects);
        }
    }
}
