// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Humanoid;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Body.Part;

public sealed partial class DashWingsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityQuery<HumanoidProfileComponent> _humanoidQuery = default!;

    [SubscribeLocalEvent]
    private void OnInserted(Entity<DashWingsComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (_timing.ApplyingState || ent.Comp.Changed)
            return;

        ent.Comp.Changed = true;
        Dirty(ent);

        if (ent.Comp.SpeciesWhitelist is { } whitelist &&
            !(_humanoidQuery.TryComp(args.Target, out var humanoid) &&
            whitelist.Contains(humanoid.Species)))
            EntityManager.AddComponents(args.Target, ent.Comp.ToAdd);
        else
            EntityManager.AddComponents(args.Target, ent.Comp.ToAddWhitelisted);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<DashWingsComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (!ent.Comp.Changed || _timing.ApplyingState)
            return;

        ent.Comp.Changed = false;
        Dirty(ent);

        if (TerminatingOrDeleted(args.Target))
            return;

        EntityManager.RemoveComponents(args.Target, ent.Comp.ToAdd);
        EntityManager.RemoveComponents(args.Target, ent.Comp.ToAddWhitelisted);
    }
}
