using Content.Medical.Common.Body;
using Content.Medical.Common.Targeting;
using Content.Shared.Body;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mobs.Systems;
using Content.Shared.Random.Helpers;
using Content.Shared.Standing;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Shared.Body;

/// <summary>
/// Trauma - organ enabled logic, extra helpers for working with body stuff
/// </summary>
public sealed partial class BodySystem
{
    [Dependency] private readonly CommonBodyCacheSystem _cache = default!;
    [Dependency] private readonly CommonBodyPartSystem _part = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    /// <summary>
    /// Body parts' organ categories.
    /// </summary>
    public static readonly ProtoId<OrganCategoryPrototype>[] BodyParts =
    [
        "Head",
        "Torso",
        "ArmLeft",
        "LegLeft",
        "HandLeft",
        "FootLeft",
        "ArmRight",
        "LegRight",
        "HandRight",
        "FootRight"
    ];

    /// <summary>
    /// Vital body parts' organ categories.
    /// </summary>
    public static readonly ProtoId<OrganCategoryPrototype>[] VitalParts =
    [
        "Head",
        "Torso"
    ];
    // TODO: vital internal organs???

    /// <summary>
    /// Tries to enable a given organ, letting systems run logic.
    /// Returns true if it is valid and now enabled.
    /// </summary>
    public bool EnableOrgan(Entity<OrganComponent?> organ)
    {
        if (!_organQuery.Resolve(organ, ref organ.Comp) || organ.Comp.Body is not {} body)
            return false;

        if (HasComp<EnabledOrganComponent>(organ))
            return true; // already enabled

        var attemptEv = new OrganEnableAttemptEvent(body);
        RaiseLocalEvent(organ, ref attemptEv);
        if (attemptEv.Cancelled)
            return false;

        EnsureComp<EnabledOrganComponent>(organ);

        // let other systems do their logic now
        var ev = new OrganEnabledEvent(body);
        RaiseLocalEvent(organ, ref ev);
        return true;
    }

    /// <summary>
    /// Disabled a given organ, letting systems run logic.
    /// Returns true if it is valid and now disabled.
    /// </summary>
    public bool DisableOrgan(Entity<OrganComponent?> organ)
    {
        if (!_organQuery.Resolve(organ, ref organ.Comp) || organ.Comp.Body is not {} body)
            return false;

        if (!TryComp<EnabledOrganComponent>(organ, out var enabled))
            return true; // already disabled

        // no attempt event that wouldn't make any sense

        RemComp(organ, enabled);

        var ev = new OrganDisabledEvent(body);
        RaiseLocalEvent(organ, ref ev);
        return true;
    }

    /// <summary>
    /// Non-dogshit version of TryGetOrgansWithComponent
    /// </summary>
    public List<Entity<T>> GetOrgans<T>(Entity<BodyComponent?> body, bool logMissing = false) where T : Component
    {
        if (!_bodyQuery.Resolve(body, ref body.Comp, logMissing))
            return [];

        TryGetOrgansWithComponent<T>(body, out var organs);
        return organs;
    }

    /// <summary>
    /// Get all organs in a body, both internal and external.
    /// </summary>
    public List<Entity<OrganComponent>> GetOrgans(Entity<BodyComponent?> body, bool logMissing = false)
        => GetOrgans<OrganComponent>(body, logMissing);

    /// <summary>
    /// Get all internal organs in a body.
    /// </summary>
    public List<Entity<InternalOrganComponent>> GetInternalOrgans(Entity<BodyComponent?> body, bool logMissing = false)
        => GetOrgans<InternalOrganComponent>(body, logMissing);

    /// <summary>
    /// Get all external organs in a body.
    /// </summary>
    public List<Entity<OrganComponent>> GetExternalOrgans(Entity<BodyComponent?> body, bool logMissing = false)
    {
        var organs = GetOrgans(body, logMissing);
        organs.RemoveAll(organ => HasComp<InternalOrganComponent>(organ));
        return organs;
    }

    /// <summary>
    /// Get a list of vital bodyparts, which contribute to vital damage.
    /// </summary>
    public List<EntityUid> GetVitalParts(EntityUid body)
    {
        // doing this to use cache instead of looping every organ and checking them
        var vital = new List<EntityUid>(VitalParts.Length);
        foreach (var category in VitalParts)
        {
            if (_cache.GetOrgan(body, category) is {} organ)
                vital.Add(organ);
        }

        return vital;
    }

    /// <summary>
    /// Gets the fraction of bodyparts that are vital.
    /// For a torso or torso+head this is 1, for invalid bodies this is 0.
    /// </summary>
    public float GetVitalBodyPartRatio(Entity<BodyComponent?> body)
    {
        if (!_bodyQuery.Resolve(body, ref body.Comp, false) || body.Comp.Organs?.ContainedEntities is not {} organs)
            return 0f;

        // TODO SHITMED: change vital to just be a bool on OrganCategoryPrototype
        int total = 0;
        int vital = 0;
        foreach (var organ in organs)
        {
            if (GetCategory(organ) is not {} category || !BodyParts.Contains(category))
                continue;

            total++;
            if (VitalParts.Contains(category))
                vital++;
        }

        return total == 0
            ? 0f // no dividing by zero incase a body somehow has no parts?!
            : (float) vital / total;
    }

    /// <summary>
    /// Converts Enums from BodyPartType to their Targeting system equivalent.
    /// </summary>
    public TargetBodyPart GetTargetBodyPart(BodyPartType type, BodyPartSymmetry symmetry)
    {
        return (type, symmetry) switch
        {
            (BodyPartType.Head, _) => TargetBodyPart.Head,
            (BodyPartType.Torso, _) => TargetBodyPart.Chest,
            (BodyPartType.Arm, BodyPartSymmetry.Left) => TargetBodyPart.LeftArm,
            (BodyPartType.Arm, BodyPartSymmetry.Right) => TargetBodyPart.RightArm,
            (BodyPartType.Hand, BodyPartSymmetry.Left) => TargetBodyPart.LeftHand,
            (BodyPartType.Hand, BodyPartSymmetry.Right) => TargetBodyPart.RightHand,
            (BodyPartType.Leg, BodyPartSymmetry.Left) => TargetBodyPart.LeftLeg,
            (BodyPartType.Leg, BodyPartSymmetry.Right) => TargetBodyPart.RightLeg,
            (BodyPartType.Foot, BodyPartSymmetry.Left) => TargetBodyPart.LeftFoot,
            (BodyPartType.Foot, BodyPartSymmetry.Right) => TargetBodyPart.RightFoot,
            _ => TargetBodyPart.Chest,
        };
    }

    /// <summary>
    /// Converts Enums from Targeting system to their BodyPartType equivalent.
    /// </summary>
    public (BodyPartType, BodyPartSymmetry) ConvertTargetBodyPart(TargetBodyPart? targetPart)
    {
        return targetPart switch
        {
            TargetBodyPart.Head => (BodyPartType.Head, BodyPartSymmetry.None),
            TargetBodyPart.Chest => (BodyPartType.Torso, BodyPartSymmetry.None),
            TargetBodyPart.Groin => (BodyPartType.Torso, BodyPartSymmetry.None),
            TargetBodyPart.LeftArm => (BodyPartType.Arm, BodyPartSymmetry.Left),
            TargetBodyPart.LeftHand => (BodyPartType.Hand, BodyPartSymmetry.Left),
            TargetBodyPart.RightArm => (BodyPartType.Arm, BodyPartSymmetry.Right),
            TargetBodyPart.RightHand => (BodyPartType.Hand, BodyPartSymmetry.Right),
            TargetBodyPart.LeftLeg => (BodyPartType.Leg, BodyPartSymmetry.Left),
            TargetBodyPart.LeftFoot => (BodyPartType.Foot, BodyPartSymmetry.Left),
            TargetBodyPart.RightLeg => (BodyPartType.Leg, BodyPartSymmetry.Right),
            TargetBodyPart.RightFoot => (BodyPartType.Foot, BodyPartSymmetry.Right),
            _ => (BodyPartType.Torso, BodyPartSymmetry.None)
        };
    }

    /// <summary>
    /// Returns an entity's organ category, or null if it isn't an organ.
    /// </summary>
    public ProtoId<OrganCategoryPrototype>? GetCategory(Entity<OrganComponent?> organ)
        => _organQuery.Resolve(organ, ref organ.Comp) ? organ.Comp.Category : null;

    /// <summary>
    /// Gets an organ in a certain slot of the body, or null if it's missing.
    /// Helper to use body cache system.
    /// </summary>
    public EntityUid? GetOrgan(EntityUid body, ProtoId<OrganCategoryPrototype> category)
        => _cache.GetOrgan(body, category);

    /// <summary>
    /// Gets the body of an organ, returning null if it isn't an organ or is detached.
    /// </summary>
    public EntityUid? GetBody(EntityUid organ)
        => _organQuery.CompOrNull(organ)?.Body;

    /// <summary>
    /// Tries to insert an organ into a body.
    /// Returns true if it is now in the body.
    /// </summary>
    public bool InsertOrgan(Entity<BodyComponent?> body, Entity<OrganComponent?> organ)
    {
        if (!_bodyQuery.Resolve(body, ref body.Comp, false) ||
            !_organQuery.Resolve(organ, ref organ.Comp) ||
            body.Comp.Organs is not {} container)
            return false;

        if (container.Contains(organ))
            return true; // it was already inserted

        var ev = new OrganInsertAttemptEvent(body, organ);
        RaiseLocalEvent(body, ref ev);
        if (!ev.Cancelled)
            RaiseLocalEvent(organ, ref ev);
        if (ev.Cancelled)
            return false;

        return _container.Insert(organ.Owner, container);
    }

    /// <summary>
    /// Tries to remove an organ from a body.
    /// Returns true if it is no longer in the body.
    /// </summary>
    public bool RemoveOrgan(Entity<BodyComponent?> body, Entity<OrganComponent?> organ)
    {
        if (!_bodyQuery.Resolve(body, ref body.Comp, false) ||
            !_organQuery.Resolve(organ, ref organ.Comp) ||
            body.Comp.Organs is not {} container)
            return false;

        if (!container.Contains(organ))
            return true; // it was already removed

        var ev = new OrganRemoveAttemptEvent(body, organ);
        RaiseLocalEvent(body, ref ev);
        if (!ev.Cancelled)
            RaiseLocalEvent(organ, ref ev);
        if (ev.Cancelled)
            return false;

        return _container.Remove(organ.Owner, container);
    }

    public bool ReplaceOrgan(Entity<BodyComponent?> body, Entity<OrganComponent?> organ)
    {
        if (!_bodyQuery.Resolve(body, ref body.Comp, false) ||
            !_organQuery.Resolve(organ, ref organ.Comp) ||
            organ.Comp.Category is not {} category)
            return false;

        // if an organ is already there try to remove it
        // it will be dropped on the floor
        if (GetOrgan(body, category) is {} old && !RemoveOrgan(body, old))
            return false;

        return InsertOrgan(body, organ);
    }

    /// <summary>
    /// Tries to decapitate a mob, returning true if it succeeded.
    /// </summary>
    public bool TryDecapitate(EntityUid uid, EntityUid? user = null)
    {
        var ev = new DecapitateEvent(user);
        RaiseLocalEvent(uid, ref ev);
        return ev.Handled;
    }

    // no marking api anymore lol have to write one myself
    #region Markings

    /// <summary>
    /// Adds a marking to an organ with a given category, not allowing duplicates on the same organ.
    /// It will have default colours.
    /// </summary>
    public bool AddOrganMarking(
        Entity<BodyComponent?> body,
        [ForbidLiteral] ProtoId<OrganCategoryPrototype> category,
        [ForbidLiteral] ProtoId<MarkingPrototype> marking,
        bool force = false)
    {
        if (GetOrgan(body, category) is not {} organ)
            return false; // no organ found

        return AddOrganMarking(organ, marking);
    }

    /// <summary>
    /// Adds a marking to a given organ, not allowing duplicates on the same organ.
    /// It will have default colours.
    /// </summary>
    public bool AddOrganMarking(
        Entity<VisualOrganMarkingsComponent?> organ,
        [ForbidLiteral] ProtoId<MarkingPrototype> marking,
        bool force = false)
    {
        if (!Resolve(organ, ref organ.Comp))
            return false; // organ doesn't support markings

        var markingData = organ.Comp.MarkingData;
        var proto = _proto.Index(marking);
        var layer = proto.BodyPart;
        if (!force)
        {
            if (proto.GroupWhitelist?.Contains(markingData.Group) == false)
                return false; // marking isn't whitelisted for this species/group

            if (!markingData.Layers.Contains(layer))
                return false; // this organ doesn't support the needed marking layer
        }

        var markings = organ.Comp.Markings;
        // ensure there's a list of markings for the given layer
        if (!markings.TryGetValue(layer, out var list))
        {
            list = [];
            markings[layer] = list;
        }

        // check for duplicates first
        foreach (var data in list)
        {
            if (data.MarkingId == marking)
                return false; // duplicate found, skip adding it
        }

        // good to go
        list.Add(new Marking(marking, []));
        Dirty(organ, organ.Comp);
        return true;
    }

    #endregion

    #region Targeting

    /// <summary>
    /// This override fetches a random body part for an entity based on the attacker's selected part, which introduces a random chance to miss
    /// so long as the entity isnt incapacitated or laying down.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="attacker"></param>
    /// <param name="targetComp"></param>
    /// <param name="attackerComp"></param>
    /// <returns></returns>
    public TargetBodyPart? GetRandomBodyPart(EntityUid target,
        EntityUid attacker,
        TargetingComponent? targetComp = null,
        TargetingComponent? attackerComp = null)
    {
        if (!Resolve(target, ref targetComp, false)
            || !Resolve(attacker, ref attackerComp, false))
            return TargetBodyPart.Chest;

        return GetRandomBodyPart(target, attackerComp.Target, targetComp);
    }

    public TargetBodyPart GetRandomBodyPart(EntityUid target,
        TargetBodyPart targetPart = TargetBodyPart.Chest,
        TargetingComponent? targetComp = null)
    {
        if (!Resolve(target, ref targetComp, false))
            return TargetBodyPart.Chest;

        if (_mob.IsIncapacitated(target)
            || _standing.IsDown(target))
            return targetPart;

        var totalWeight = targetComp.TargetOdds[targetPart].Values.Sum();
        var seed = SharedRandomExtensions.HashCodeCombine((int) _timing.CurTick.Value, GetNetEntity(target).Id);
        var rand = new System.Random(seed);
        var randomValue = rand.NextFloat() * totalWeight;

        foreach (var (part, weight) in targetComp.TargetOdds[targetPart])
        {
            if (randomValue <= weight)
                return part;
            randomValue -= weight;
        }

        return TargetBodyPart.Chest;
    }

    public TargetBodyPart GetRandomBodyPart(EntityUid target)
    {
        var children = GetVitalParts(target);
        if (children.Count == 0)
            return TargetBodyPart.Chest;

        var seed = SharedRandomExtensions.HashCodeCombine((int) _timing.CurTick.Value, GetNetEntity(target).Id);
        var rand = new System.Random(seed);
        return _part.GetTargetBodyPart(rand.Pick(children)) ?? TargetBodyPart.Chest;
    }

    public TargetBodyPart GetRandomBodyPart(EntityUid target,
        EntityUid? attacker,
        TargetBodyPart? targetPart = null,
        TargetingComponent? targetComp = null)
    {
        if (!Resolve(target, ref targetComp, false))
            return TargetBodyPart.Chest;

        if (targetPart.HasValue)
            return GetRandomBodyPart(target, targetPart: targetPart.Value);

        if (attacker.HasValue
            && TryComp(attacker.Value, out TargetingComponent? attackerComp))
            return GetRandomBodyPart(target, targetPart: attackerComp.Target);

        return GetRandomBodyPart(target);
    }

    public TargetBodyPart GetTargetBodyPart(EntityUid target,
        EntityUid? attacker,
        TargetBodyPart? targetPart = null,
        TargetingComponent? targetComp = null)
    {
        if (!Resolve(target, ref targetComp, false))
            return TargetBodyPart.Chest;

        if (targetPart.HasValue)
            return targetPart.Value;

        if (attacker.HasValue
            && TryComp(attacker.Value, out TargetingComponent? attackerComp))
            return attackerComp.Target;

        return GetRandomBodyPart(target);
    }

    #endregion
}
