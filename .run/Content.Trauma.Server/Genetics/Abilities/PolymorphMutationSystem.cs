// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid;
using Content.Trauma.Shared.Genetics.Abilities;
using Content.Trauma.Shared.Genetics.Mutations;
using Content.Server.Polymorph.Systems;

namespace Content.Trauma.Server.Genetics.Abilities;

public sealed partial class PolymorphMutationSystem : EntitySystem
{
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private EntityQuery<HumanoidProfileComponent> _humanoidQuery = default!;

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
        if (!_humanoidQuery.TryComp(target, out var humanoid) ||
            !ent.Comp.Prototypes.TryGetValue(humanoid.Species, out var proto))
            return; // people/monkeys/kobolds only!

        if (_polymorph.PolymorphEntity(target, proto) == null)
            return;

        ent.Comp.Worked = true;
    }

    private void OnMutationRemoved(Entity<PolymorphMutationComponent> ent, ref MutationRemovedEvent args)
    {
        if (args.Automatic)
            return;

        var target = args.Target.Owner;
        if (ent.Comp.Worked)
            _polymorph.Revert(target);
        else if (_humanoidQuery.TryComp(target, out var humanoid) && ent.Comp.Reverts.TryGetValue(humanoid.Species, out var revert))
            _polymorph.PolymorphEntity(target, revert);
        else if (ent.Comp.Fallback is {} fallback)
            _polymorph.PolymorphEntity(target, fallback);
    }
}
