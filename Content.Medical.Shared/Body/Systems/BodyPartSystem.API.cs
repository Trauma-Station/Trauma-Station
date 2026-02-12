// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Medical.Common.Targeting;
using Content.Shared.Body;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Medical.Shared.Body;

/// <summary>
/// Public API for bodyparts.
/// </summary>
public sealed partial class BodyPartSystem
{
    /// <summary>
    /// Returns true if a part matches a given type and symmetry.
    /// A null parameter means allow any value.
    /// </summary>
    public static bool PartMatches(BodyPartComponent part, BodyPartType? partType = null, BodyPartSymmetry? symmetry = null)
        => (partType == null || part.PartType == partType) && (symmetry == null || part.Symmetry == symmetry);

    /// <summary>
    /// Get a list of every body part attached to a body.
    /// </summary>
    public List<Entity<BodyPartComponent>> GetBodyParts(Entity<BodyComponent?> body)
        => _body.GetOrgans<BodyPartComponent>(body);

    public override List<EntityUid> GetBodyParts(EntityUid body, BodyPartType? partType, BodyPartSymmetry? symmetry = null)
    {
        var parts = new List<EntityUid>();
        foreach (var part in GetBodyParts(body))
        {
            if (PartMatches(part.Comp, partType, symmetry))
                parts.Add(part.Owner);
        }
        return parts;
    }

    /// <summary>
    /// Get a dictionary of every organ and bodypart parented to the given part, indexed by organ category.
    /// Does nothing if the part is severed.
    /// </summary>
    public Dictionary<ProtoId<OrganCategoryPrototype>, Entity<OrganComponent>> GetPartOrgans(Entity<BodyPartComponent?> part)
    {
        if (!_query.Resolve(part, ref part.Comp) ||
            _body.GetBody(part.Owner) is not {} body)
            return [];

        var organs = new Dictionary<ProtoId<OrganCategoryPrototype>, Entity<OrganComponent>>();
        foreach (var (category, organ) in part.Comp.Children)
        {
            if (_organQuery.TryComp(organ, out var organComp))
                organs[category] = (organ, organComp);
        }
        return organs;
    }

    public EntityUid? GetParentPart(Entity<ChildOrganComponent?> organ)
        => _childQuery.Resolve(organ, ref organ.Comp, false) ? organ.Comp.Parent : null;

    /// <summary>
    /// Get the outermost organ for the given organ.
    /// For a part this is the part itself.
    /// For an internal organ this is the part containing it.
    /// </summary>
    public EntityUid GetOuterOrgan(Entity<ChildOrganComponent?> organ)
        // fallback to the organ if it's already a part, bad prototype or not attached to a parent
        => !_query.HasComp(organ) && GetParentPart(organ) is {} parent
            ? parent
            : organ;

    /// <summary>
    /// Finds the first body part matching a given type and symmetry.
    /// </summary>
    public Entity<BodyPartComponent>? FindBodyPart(Entity<BodyComponent?> body, BodyPartType? partType = null, BodyPartSymmetry? symmetry = null)
    {
        foreach (var part in GetBodyParts(body))
        {
            if (PartMatches(part, partType, symmetry))
                return part;
        }

        return null;
    }

    /// <summary>
    /// Checks whether the part is valid and has a slot for a given organ category.
    /// </summary>
    public bool HasOrganSlot(Entity<BodyPartComponent?> ent, [ForbidLiteral] ProtoId<OrganCategoryPrototype> category)
        => _query.Resolve(ent, ref ent.Comp) && ent.Comp.Slots.Contains(category);

    /// <summary>
    /// Checks whether the part is valid and has an organ for a given category.
    /// </summary>
    public bool HasOrgan(Entity<BodyPartComponent?> ent, [ForbidLiteral] ProtoId<OrganCategoryPrototype> category)
        => _query.Resolve(ent, ref ent.Comp) && ent.Comp.Children.ContainsKey(category);

    /// <summary>
    /// Tries to get an organ from a part's slots.
    /// </summary>
    public EntityUid? GetOrgan(Entity<BodyPartComponent?> ent, [ForbidLiteral] ProtoId<OrganCategoryPrototype> category)
        => _query.Resolve(ent, ref ent.Comp) && ent.Comp.Children.TryGetValue(category, out var organ)
            ? organ
            : null;

    public bool TryAddSlot(Entity<BodyPartComponent?> ent, [ForbidLiteral] ProtoId<OrganCategoryPrototype> category)
    {
        if (!_query.Resolve(ent, ref ent.Comp))
            return false;

        DebugTools.Assert(_body.GetCategory(ent.Owner) != category,
            $"Tried to add {ToPrettyString(ent)}'s own category {category} to its slots!");
        if (!ent.Comp.Slots.Add(category))
            return false;

        DirtyField(ent, ent.Comp, nameof(BodyPartComponent.Slots));
        return true;
    }

    public override bool TryAddSlot(EntityUid uid, [ForbidLiteral] string category)
        => _query.TryComp(uid, out var comp) && TryAddSlot((uid, comp), category);

    public override BodyPartType? GetPartType(EntityUid uid)
        => _query.CompOrNull(uid)?.PartType;

    public override BodyPartSymmetry GetSymmetry(EntityUid uid)
        => _query.CompOrNull(uid)?.Symmetry ?? BodyPartSymmetry.None;

    public override TargetBodyPart? GetTargetBodyPart(EntityUid uid)
        => _query.TryComp(uid, out var part)
            ? _body.GetTargetBodyPart(part.PartType, part.Symmetry)
            : null;

    /// <summary>
    /// Tries to remove an organ slot from this bodypart.
    /// The slot can be for internal and external organs.
    /// </summary>
    /// <returns>true if the part is valid and previously had the slot</returns>
    public bool TryRemoveSlot(Entity<BodyPartComponent?> ent, [ForbidLiteral] ProtoId<OrganCategoryPrototype> category)
    {
        if (!_query.Resolve(ent, ref ent.Comp))
            return false;

        if (!ent.Comp.Slots.Remove(category))
            return false;

        DirtyField(ent, ent.Comp, nameof(BodyPartComponent.Slots));

        if (ent.Comp.Children.TryGetValue(category, out var organ))
        {
            if (_body.GetBody(ent.Owner) is {} body && !_body.RemoveOrgan(body, organ))
                Log.Error($"When removing organ slot {category} from {ToPrettyString(body)}'s {ToPrettyString(ent)}, {ToPrettyString(organ)} could not be removed!");
            // TODO SHITMED: also eject if it's in the severed container
            OrganRemoved(ent, organ); // incase there is no body or it somehow failed to call it
        }

        return true;
    }

    /// <summary>
    /// Tries to insert an organ/child part into a parent bodypart.
    /// If the part is not attached to a body, it will.
    /// The organ must be in the part's slots.
    /// </summary>
    public bool CanInsertOrgan(Entity<BodyPartComponent?> part, [ForbidLiteral] ProtoId<OrganCategoryPrototype> category)
        => _query.Resolve(part, ref part.Comp) &&
            // the part needs a slot
            part.Comp.Slots.Contains(category) &&
            // the slot can't already be occupied
            !part.Comp.Children.ContainsKey(category);

    /// <summary>
    /// Tries to insert an organ into the part's body or, if it is severed, into its organs container.
    /// </summary>
    public bool InsertOrgan(Entity<BodyPartComponent?> part, Entity<OrganComponent?> organ)
    {
        if (!_query.Resolve(part, ref part.Comp) ||
            _body.GetCategory(organ) is not {} category ||
            !CanInsertOrgan(part, category))
            return false;

        if (_body.GetBody(part.Owner) is {} body)
            return _body.InsertOrgan(body, organ);

        if (GetSeveredOrgansContainer(part) is {} container)
            return _container.Insert(organ.Owner, container);

        Log.Error($"{ToPrettyString(part)} was neither attached to a body nor severed when trying to insert {ToPrettyString(organ)}!?");
        return false;
    }

    /// <summary>
    /// Tries to remove an organ from the part's body or, if it is severed, its organs container.
    /// </summary>
    public bool RemoveOrgan(Entity<BodyPartComponent?> part, Entity<OrganComponent?> organ)
    {
        if (!_query.Resolve(part, ref part.Comp) ||
            _body.GetCategory(organ) is not {} category ||
            // couldnt' be inserted anyway
            !part.Comp.Slots.Contains(category))
            return false;

        if (_body.GetBody(part.Owner) is {} body)
            return _body.RemoveOrgan(body, organ);

        if (GetSeveredOrgansContainer(part) is {} container)
            return _container.Remove(organ.Owner, container);

        Log.Error($"{ToPrettyString(part)} was neither attached to a body nor severed when trying to remove {ToPrettyString(organ)}!?");
        return false;
    }

    /// <summary>
    /// Gets the severed organs container for a bodypart.
    /// Returns null if the bodypart is invalid or not severed.
    /// </summary>
    public Container? GetSeveredOrgansContainer(Entity<BodyPartComponent?> ent)
    => _query.Resolve(ent, ref ent.Comp) &&
        // won't exist unless the part is severed
        _container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var baseContainer) &&
        baseContainer is Container container
        ? container
        : null;

    /// <summary>
    /// Gets or creates a part's severed organs container.
    /// Should only be used if the part has actually been severed.
    /// </summary>
    public Container EnsureSeveredOrgansContainer(Entity<BodyPartComponent> ent)
        => _container.EnsureContainer<Container>(ent.Owner, ent.Comp.ContainerId);
}
