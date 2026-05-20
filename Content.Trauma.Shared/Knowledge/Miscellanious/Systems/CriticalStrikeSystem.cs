// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.EntityEffects;
using Content.Shared.Random.Helpers;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Miscellanious.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Knowledge.Miscellanious.Systems;

public sealed partial class CriticalStrikeSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedEntityEffectsSystem _effect = default!;

    private static readonly ProtoId<CriticalStrikePrototype> StandardMelee = "StandardMelee";
    private static readonly ProtoId<FumblePrototype> StandardFumble = "StandardFumble";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, CriticalHitEvent>(OnCriticalHit);
        SubscribeLocalEvent<KnowledgeHolderComponent, OnFumbleEvent>(OnFumble);
    }

    private void OnCriticalHit(Entity<KnowledgeHolderComponent> ent, ref CriticalHitEvent args)
    {
        var table = _proto.Index(StandardMelee);

        foreach (var damage in args.Damage.DamageDict)
        {
            if (!table.Entries.TryGetValue(damage.Key, out var entries))
                return;

            var effect = entries
                .OrderBy(e => e.MinThreshold)
                .FirstOrDefault(e => damage.Value <= e.MinThreshold);

            if (effect is { })
                _effect.ApplyEffects(ent, effect.Effects, 1.0f, args.Attacker);
        }
    }

    private void OnFumble(Entity<KnowledgeHolderComponent> ent, ref OnFumbleEvent args)
    {
        var table = _proto.Index<FumblePrototype>(StandardFumble);

        var roll = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent)).Next(1, 1000) + args.FumbleDifference * 10;

        var fumble = table.Entries
            .OrderBy(e => e.MinThreshold)
            .FirstOrDefault(e => roll <= e.MinThreshold);
    }
}
