// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Shared.Body;
using Content.Shared.Gibbing;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Medical.Shared.Body;

/// <summary>
/// System that handles bodypart logic and provides API for working with them.
/// </summary>
public sealed partial class BodyPartSystem : CommonBodyPartSystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private EntityQuery<BodyPartComponent> _query;
    private EntityQuery<ChildOrganComponent> _childQuery;
    private EntityQuery<OrganComponent> _organQuery;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<BodyPartComponent>();
        _childQuery = GetEntityQuery<ChildOrganComponent>();
        _organQuery = GetEntityQuery<OrganComponent>();

        SubscribeLocalEvent<BodyPartComponent, OrganGotInsertedEvent>(OnPartInserted);
        SubscribeLocalEvent<BodyPartComponent, OrganGotRemovedEvent>(OnPartRemoved);
        SubscribeLocalEvent<BodyPartComponent, BeingGibbedEvent>(OnBeingGibbed);
    }

    private void OnPartInserted(Entity<BodyPartComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        // fresh part, no organs inside
        if (GetSeveredOrgansContainer(ent.AsNullable()) is not {} container)
            return;

        var body = args.Target.AsNullable();
        var organs = new List<EntityUid>(container.ContainedEntities); // no CME
        foreach (var organ in organs)
        {
            if (_body.InsertOrgan(body, organ))
                continue;

            Log.Error($"Couldn't insert {ToPrettyString(organ)} from {ToPrettyString(ent)} back into {ToPrettyString(args.Target)} after being attached, ejecting it");
            if (!_container.Remove(organ, container))
                Log.Error($"Organ {ToPrettyString(organ)} got stuck inside of {ToPrettyString(ent)} after being inserted into {ToPrettyString(args.Target)}");
        }
    }

    private void OnPartRemoved(Entity<BodyPartComponent> ent, ref OrganGotRemovedEvent args)
    {
        // don't transfer parts if the body is being deleted
        // note that this will still transfer if the part is being deleted, so its organs will go away too
        if (TerminatingOrDeleted(ent) || _timing.ApplyingState)
            return;

        var container = EnsureSeveredOrgansContainer(ent);
        var body = args.Target.AsNullable();
        foreach (var category in ent.Comp.Slots)
        {
            // empty slot, don't care
            if (_body.GetOrgan(body, category) is not {} organ)
                continue;

            // slot has an organ so try to put it in the container
            if (!_container.Insert(organ, container))
            {
                // probably from failing to be removed, suspicious
                Log.Error($"Failed to store {ToPrettyString(ent)}'s {category} organ {ToPrettyString(organ)}!");
                continue;
            }
        }
    }

    private void OnBeingGibbed(Entity<BodyPartComponent> ent, ref BeingGibbedEvent args)
    {
        if (GetSeveredOrgansContainer(ent.AsNullable()) is not {} container)
            return;

        // gibbing a severed head spills its brains out >:D
        foreach (var organ in container.ContainedEntities)
        {
            args.Giblets.Add(organ);
        }
    }

    internal void OrganInserted(Entity<BodyPartComponent?> part, Entity<OrganComponent?> organ)
    {
        DebugTools.Assert(part.Owner != organ.Owner);
        if (!_query.Resolve(part, ref part.Comp) ||
            _body.GetCategory(organ) is not {} category ||
            !CanInsertOrgan(part, category)) // just incase
            return;

        part.Comp.Children[category] = organ;
        DirtyField(part, part.Comp, nameof(BodyPartComponent.Children));

        var ev = new OrganInsertedIntoPartEvent(organ, category);
        RaiseLocalEvent(part, ref ev);
    }

    internal void OrganRemoved(Entity<BodyPartComponent?> part, Entity<OrganComponent?> organ)
    {
        DebugTools.Assert(part.Owner != organ.Owner);
        if (!_query.Resolve(part, ref part.Comp) ||
            _body.GetCategory(organ) is not {} category)
            return;

        part.Comp.Children.Remove(category);
        DirtyField(part, part.Comp, nameof(BodyPartComponent.Children));

        var ev = new OrganRemovedFromPartEvent(organ, category);
        RaiseLocalEvent(part, ref ev);
    }
}
