// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Shared.Body;
using Content.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Body;

/// <summary>
/// Handles events to properly cache organs in a body with <see cref="BodyCacheComponent"/> and <see cref="ChildOrganComponent"/>.
/// </summary>
public sealed class BodyCacheSystem : CommonBodyCacheSystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly BodyPartSystem _part = default!;

    private EntityQuery<BodyCacheComponent> _query;
    private EntityQuery<ChildOrganComponent> _childQuery;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<BodyCacheComponent>();
        _childQuery = GetEntityQuery<ChildOrganComponent>();

        // adding BodyCache automatically, carefully using different events than BodySystem does for containers
        SubscribeLocalEvent<BodyComponent, ComponentStartup>(OnBodyStartup);
        SubscribeLocalEvent<BodyComponent, ComponentRemove>(OnBodyRemove);

        SubscribeLocalEvent<BodyCacheComponent, OrganInsertedIntoEvent>(OnBodyInsertedInto);
        SubscribeLocalEvent<BodyCacheComponent, OrganRemovedFromEvent>(OnBodyRemovedFrom);
        SubscribeLocalEvent<BodyCacheComponent, MapInitEvent>(OnMapInit,
            after: [ typeof(ContainerFillSystem), typeof(InitialBodySystem) ]); // only run after all organs have been added so it's complete

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

    private void OnMapInit(Entity<BodyCacheComponent> ent, ref MapInitEvent args)
    {
        foreach (var organ in ent.Comp.Organs.Values)
        {
            // ignore torso or parts which happened to be added after their parent
            if (!_childQuery.TryComp(organ, out var child) || child.Parent != null)
                continue;

            child.Parent = GetOrgan(ent.AsNullable(), child.ParentCategory);
            if (child.Parent is not {} parent)
            {
                Log.Error($"Organ {ToPrettyString(organ)} expected a parent of {child.ParentCategory} but none was found in {ToPrettyString(ent)}!");
                continue;
            }

            Dirty(organ, child);

            // let the part track its child too
            _part.OrganInserted(parent, organ);
        }
    }

    private void OnChildInsertAttempt(Entity<ChildOrganComponent> ent, ref OrganInsertAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (_body.GetCategory(ent.Owner) is not {} category ||
            GetOrgan(args.Body, ent.Comp.ParentCategory) is not {} part ||
            !_part.CanInsertOrgan(part, category))
            args.Cancelled = true;
    }

    private void OnChildInserted(Entity<ChildOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        // will only reliably work during surgery, OnMapInit ensures they all find their parents on spawn
        ent.Comp.Parent = GetOrgan(args.Target.Owner, ent.Comp.ParentCategory);
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
        _body.EnableOrgan(ent.AsNullable());
    }

    private void OnRemoved(Entity<OrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        _body.DisableOrgan(ent.AsNullable());
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

    public override EntityUid? GetOrgan(EntityUid body, [ForbidLiteral] string category)
        => _query.TryComp(body, out var comp)
            ? GetOrgan((body, comp), category)
            : null;

    #endregion
}
