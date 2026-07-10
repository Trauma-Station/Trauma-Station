// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Trauma.Shared.Tackle;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Body.Part;

public sealed partial class DashWingsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityQuery<HumanoidProfileComponent> _humanoidQuery = default!;
    [Dependency] private EntityQuery<TacklerComponent> _tacklerQuery = default!;

    [SubscribeLocalEvent]
    private void OnInserted(Entity<DashWingsComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (_timing.ApplyingState || ent.Comp.Changed || !_tacklerQuery.TryComp(args.Target, out var tackler))
            return;

        ent.Comp.Changed = true;
        tackler.SkillMod += ent.Comp.SkillMod;
        tackler.Range += ent.Comp.RangeModifier;
        Dirty(ent);
        Dirty(args.Target, tackler);

        EntityManager.AddComponents(args.Target, ent.Comp.ToAdd);

        if (ent.Comp.SpeciesWhitelist is { } whitelist &&
            !(_humanoidQuery.TryComp(args.Target, out var humanoid) &&
            whitelist.Contains(humanoid.Species)))
            return;

        tackler.KnockdownUser = false;
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<DashWingsComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (!ent.Comp.Changed || _timing.ApplyingState)
            return;

        ent.Comp.Changed = false;
        Dirty(ent);

        if (TerminatingOrDeleted(args.Target) || !_tacklerQuery.TryComp(args.Target, out var tackler))
            return;

        tackler.SkillMod -= ent.Comp.SkillMod;
        tackler.Range -= ent.Comp.RangeModifier;
        tackler.KnockdownUser = true;
        Dirty(args.Target, tackler);

        EntityManager.RemoveComponents(args.Target, ent.Comp.ToAdd);
    }
}
