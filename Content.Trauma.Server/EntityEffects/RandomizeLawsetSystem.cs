// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Silicons.Laws;
using Content.Shared.EntityEffects;
using Content.Shared.Silicons.Laws.Components;
using Content.Trauma.Shared.EntityEffects;
using Robust.Shared.Random;

namespace Content.Trauma.Server.EntityEffects;

public sealed class RandomizeLawsetSystem : EntityEffectSystem<SiliconLawProviderComponent, RandomizeLawset>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SiliconLawSystem _law = default!;

    protected override void Effect(Entity<SiliconLawProviderComponent> ent, ref EntityEffectEvent<RandomizeLawset> args)
    {
        var lawset = _random.Pick(args.Effect.Lawsets);
        var laws = _law.GetLawset(lawset).Laws;
        _law.SetLaws(laws, ent);
    }
}
