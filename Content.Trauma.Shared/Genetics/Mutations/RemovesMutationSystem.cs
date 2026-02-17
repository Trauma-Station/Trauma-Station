// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Shared.Genetics.Mutations;

public sealed class RemovesMutationSystem : EntitySystem
{
    [Dependency] private readonly MutationSystem _mutation = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RemovesMutationComponent, MutationAddedEvent>(OnAdded);
    }

    private void OnAdded(Entity<RemovesMutationComponent> ent, ref MutationAddedEvent args)
    {
        foreach (var id in ent.Comp.Removes)
        {
            _mutation.RemoveMutation(args.Target.AsNullable(), id, user: args.User, automatic: args.Automatic, predicted: args.Predicted);
        }
    }
}
