// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Shared.Body;
using Content.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Medical.Shared.Body;

/// <summary>
/// Handles events to properly cache organs in a body with <see cref="BodyCacheComponent"/> and <see cref="ChildOrganComponent"/>.
/// </summary>
public sealed class BodyCacheSystem : CommonBodyCacheSystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly BodyPartSystem _part = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityQuery<BodyCacheComponent> _query = default!;
    [Dependency] private readonly EntityQuery<ChildOrganComponent> _childQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        // adding BodyCache automatically, carefully using different events than BodySystem does for containers
        SubscribeLocalEvent<BodyComponent, ComponentStartup>(OnBodyStartup);
        SubscribeLocalEvent<BodyComponent, ComponentRemove>(OnBodyRemove);

        SubscribeLocalEvent<BodyCacheComponent, OrganInsertedIntoEvent>(OnBodyInsertedInto);
        SubscribeLocalEvent<BodyCacheComponent, OrganRemovedFromEvent>(OnBodyRemovedFrom);
        SubscribeLocalEvent<BodyCacheComponent, BodyInitEvent>(OnBodyInit);

        SubscribeLocalEvent<ChildOrganComponent, OrganInsertAttemptEvent>(OnChildInsertAttempt);
        SubscribeLocalEvent<ChildOrganComponent, OrganGotInsertedEvent>(OnChildInserted);
        SubscribeLocalEvent<ChildOrganComponent, OrganGotRemovedEvent>(OnChildRemoved);

        // not really best place for it but idc
        SubscribeLocalEvent<OrganComponent, OrganGotInsertedEvent>(OnInserted);
        SubscribeLocalEvent<OrganComponent, OrganGotRemovedEvent>(OnRemoved);
    }

    private void OnBodyStartup(Entity<BodyComponent> ent, ref ComponentStartup args)
    {
        // this will fail linter if you leave BodyCache from a Body prototype
        EnsureComp<BodyCacheComponent>(ent);
    }

    private void OnBodyRemove(Entity<BodyComponent> ent, ref ComponentRemove args)
    {
        if (_query.TryComp(ent, out var cache)) // prevent deleting already deleted comp
            RemCompDeferred(ent, cache);
    }

    private void OnBodyInsertedInto(Entity<BodyCacheComponent> ent, ref OrganInsertedIntoEvent args)
    {
        if (args.Organ.Comp.Category is not {} category)
            return;

        ent.Comp.Organs[category] = args.Organ;
        Dirty(ent);
    }

    private void OnBodyRemovedFrom(Entity<BodyCacheComponent> ent, ref OrganRemovedFromEvent args)
    {
        if (TerminatingOrDeleted(ent) || args.Organ.Comp.Category is not {} category)
            return;

        ent.Comp.Organs.Remove(category);
        Dirty(ent);
    }

    private void OnBodyInit(Entity<BodyCacheComponent> ent, ref BodyInitEvent args)
    {
        foreach (var organ in ent.Comp.Organs.Values)
        {
            // ignore torso or parts which happened to be added after their parent
            if (!_childQuery.TryComp(organ, out var child) || child.Parent != null)
                continue;

            child.Parent = GetOrgan(ent.AsNullable(), child.Parents);
            if (child.Parent is not {} parent)
            {
                Log.Error($"Organ {ToPrettyString(organ)} expected a parent of {child.Parents[0]} but none was found in {ToPrettyString(ent)}!");
                continue;
            }

            Dirty(organ, child);

            // let the part track its child too
            _part.OrganInserted(parent, organ);
        }
    }

    private void OnChildInsertAttempt(Entity<ChildOrganComponent> ent, ref OrganInsertAttemptEvent args)
    {
        if (args.Cancelled || ent.Owner != args.Organ)
            return;

        if (_body.GetCategory(ent.Owner) is not {} category ||
            GetParentOrgan(args.Body, category, ent.Comp.Parents) == null)
            args.Cancelled = true;
    }

    private void OnChildInserted(Entity<ChildOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (_body.GetCategory(ent.Owner) is not {} category)
            return;

        // will only reliably work during surgery, OnBodyInit ensures they all find their parents on spawn
        ent.Comp.Parent = GetParentOrgan(args.Target.Owner, category, ent.Comp.Parents);
        Dirty(ent);

        // only reason this would not exist is:
        // - container fill, don't care MapInit will fix it
        // - a shitter adding side effects to insert attempt, your fault for doing that
        if (ent.Comp.Parent is not {} part)
            return;

        _part.OrganInserted(part, ent.Owner);
    }

    private void OnChildRemoved(Entity<ChildOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (ent.Comp.Parent is {} part && !TerminatingOrDeleted(part))
            _part.OrganRemoved(part, ent.Owner);

        ent.Comp.Parent = null;
        Dirty(ent);
    }

    // so you dont need duplicate events for insert/enable and it auto updates on surgery
    private void OnInserted(Entity<OrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (!_timing.ApplyingState) // components are networked this doesnt need to get trolled
            _body.EnableOrgan(ent.AsNullable(), args.Target); // have to pass the body because it's null until after the events are raised
    }

    private void OnRemoved(Entity<OrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (!_timing.ApplyingState)
            _body.DisableOrgan(ent.AsNullable(), args.Target);
    }

    #region Public API

    /// <summary>
    /// Returns true if an organ is cached.
    /// </summary>
    public bool HasOrgan(Entity<BodyCacheComponent?> ent, [ForbidLiteral] ProtoId<OrganCategoryPrototype> category)
        => _query.Resolve(ent, ref ent.Comp, false) && ent.Comp.Organs.ContainsKey(category);

    /// <summary>
    /// Get the cached organ for this body and organ category.
    /// </summary>
    public EntityUid? GetOrgan(Entity<BodyCacheComponent?> ent, [ForbidLiteral] ProtoId<OrganCategoryPrototype> category)
        => _query.Resolve(ent, ref ent.Comp, false) && ent.Comp.Organs.TryGetValue(category, out var organ)
            ? organ
            : null;

    /// <summary>
    /// Get the first cached organ for this body and a list of allowed categories.
    /// </summary>
    public EntityUid? GetOrgan(Entity<BodyCacheComponent?> ent, IReadOnlyList<ProtoId<OrganCategoryPrototype>> categories)
    {
        if (!_query.Resolve(ent, ref ent.Comp, false))
            return null;

        foreach (var category in categories)
        {
            if (ent.Comp.Organs.TryGetValue(category, out var organ))
                return organ;
        }

        return null;
    }

    /// <summary>
    /// Try get the first cached organ for this body and a list of allowed categories, which can add a given child organ.
    /// </summary>
    public EntityUid? GetParentOrgan(Entity<BodyCacheComponent?> ent,
        [ForbidLiteral] ProtoId<OrganCategoryPrototype> child,
        IReadOnlyList<ProtoId<OrganCategoryPrototype>> categories)
    {
        if (!_query.Resolve(ent, ref ent.Comp, false))
            return null;

        foreach (var category in categories)
        {
            if (ent.Comp.Organs.TryGetValue(category, out var organ) && _part.CanInsertOrgan(organ, child))
                return organ;
        }

        return null;
    }

    public override EntityUid? GetOrgan(EntityUid body, [ForbidLiteral] string category)
        => _query.TryComp(body, out var comp)
            ? GetOrgan((body, comp), category)
            : null;

    /// <summary>
    /// Sets a child organ's allowed parents to a single category.
    /// </summary>
    public void SetParentCategory(Entity<ChildOrganComponent?> organ, [ForbidLiteral] ProtoId<OrganCategoryPrototype> category)
    {
        if (!_childQuery.Resolve(organ, ref organ.Comp) || organ.Comp.Parents.Count == 1 && organ.Comp.Parents[0] == category)
            return;

        organ.Comp.Parents.Clear();
        organ.Comp.Parents.Add(category);
        Dirty(organ, organ.Comp);
    }

    /// <summary>
    /// Changes a child organ's parent to a different part.
    /// It is assumed that they are in the same body, only runs part-specific logic.
    /// </summary>
    public void SetParent(Entity<ChildOrganComponent?> organ, EntityUid parent)
    {
        if (!_childQuery.Resolve(organ, ref organ.Comp) || organ.Comp.Parent == parent)
            return;

        if (organ.Comp.Parent is {} old)
            _part.OrganRemoved(old, organ.Owner);
        organ.Comp.Parent = parent;
        Dirty(organ);
        _part.OrganInserted(parent, organ.Owner);
    }

    #endregion
}
