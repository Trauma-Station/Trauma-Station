using Content.Trauma.Shared.Genetics.Abilities;
using Content.Trauma.Shared.Genetics.Mutations;
using Content.Server.Polymorph.Systems;

namespace Content.Trauma.Server.Genetics.Abilities;

public sealed class PolymorphMutationSystem : EntitySystem
{
    [Dependency] private readonly PolymorphSystem _polymorph = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PolymorphMutationComponent, MutationAddedEvent>(OnMutationAdded);
        SubscribeLocalEvent<PolymorphMutationComponent, MutationRemovedEvent>(OnMutationRemoved);
    }

    private void OnMutationAdded(Entity<PolymorphMutationComponent> ent, ref MutationAddedEvent args)
    {
        // polymorph automatically moves mutations so do nothing or it would be in some kind of hell
        if (args.Automatic)
            return;

        var target = args.Target.Owner;
        if (_polymorph.PolymorphEntity(target, ent.Comp.Prototype) == null)
            return;

        ent.Comp.Worked = true;
    }

    private void OnMutationRemoved(Entity<PolymorphMutationComponent> ent, ref MutationRemovedEvent args)
    {
        if (ent.Comp.Worked && !args.Automatic)
            _polymorph.Revert(args.Target.Owner);
    }
}
