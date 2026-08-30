// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Random.Helpers;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Adds a random negative mutation to the target entity.
/// Does nothing for mutations which are already present or conflict with existing ones.
/// </summary>
public sealed partial class RandomNegativeMutation : EntityEffectBase<RandomNegativeMutation>;

public sealed partial class RandomNegativeMutationEffectSystem : EntityEffectSystem<MutatableComponent, RandomNegativeMutation>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MutationSystem _mutation = default!;

    protected override void Effect(Entity<MutatableComponent> ent, ref EntityEffectEvent<RandomNegativeMutation> args)
    {
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        var mutation = rand.Pick(_mutation.NegativeMutations);
        _mutation.AddMutation(ent.AsNullable(), mutation, user: args.User,
            automatic: false, predicted: args.Predicted);
    }
}
