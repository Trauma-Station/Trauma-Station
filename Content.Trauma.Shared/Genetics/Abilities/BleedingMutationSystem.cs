// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Trauma.Shared.Genetics.Mutations;

namespace Content.Trauma.Shared.Genetics.Abilities;

public sealed partial class BleedingMutationSystem : EntitySystem
{
    [Dependency] private SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private EntityQuery<BloodstreamComponent> _bloodstreamQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BleedingMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<BleedingMutationComponent, MutationRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<BleedingMutationComponent, BleedModifierEvent>(OnBleedModifier);
    }

    private void OnAdded(Entity<BleedingMutationComponent> ent, ref MutationAddedEvent args)
    {
        if (!_bloodstreamQuery.TryComp(args.Target, out var blood))
            return;

        _bloodstream.SetRefreshAmount((args.Target, blood), blood.BloodRefreshAmount * ent.Comp.RefreshModifier);
    }

    private void OnRemoved(Entity<BleedingMutationComponent> ent, ref MutationRemovedEvent args)
    {
        if (!_bloodstreamQuery.TryComp(args.Target, out var blood))
            return;

        _bloodstream.SetRefreshAmount((args.Target, blood), blood.BloodRefreshAmount / ent.Comp.RefreshModifier);
    }

    private void OnBleedModifier(Entity<BleedingMutationComponent> ent, ref BleedModifierEvent args)
    {
        args.BleedAmount *= ent.Comp.BleedModifier;
    }
}
