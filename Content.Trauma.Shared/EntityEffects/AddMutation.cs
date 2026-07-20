// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Genetics.Mutations;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Adds a mutation to the target entity.
/// </summary>
public sealed partial class AddMutation : EntityEffectBase<AddMutation>
{
    /// <summary>
    /// The mutation to add.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<MutationComponent> Mutation;

    /// <summary>
    /// Hide popups for the target if true.
    /// </summary>
    [DataField]
    public bool Automatic;
}

public sealed partial class AddMutationEffectSystem : EntityEffectSystem<MutatableComponent, AddMutation>
{
    [Dependency] private MutationSystem _mutation = default!;

    protected override void Effect(Entity<MutatableComponent> ent, ref EntityEffectEvent<AddMutation> args)
    {
        var e = args.Effect;
        _mutation.AddMutation(ent.AsNullable(), e.Mutation, user: args.User,
            automatic: e.Automatic, predicted: args.Predicted);
    }
}
