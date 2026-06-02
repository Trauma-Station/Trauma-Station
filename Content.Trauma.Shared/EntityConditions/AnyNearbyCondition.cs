// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Condition that checks if any entities are nearby.
/// </summary>
public sealed partial class AnyNearbyCondition : EntityConditionBase<AnyNearbyCondition>
{
    /// <summary>
    /// The component to use for lookups.
    /// If this is Transform it will find any entity in range.
    /// Use the rarest component you can for best performance.
    /// You don't need to include this in <see cref="Whitelist"/>.
    /// </summary>
    [DataField(required: true)]
    public string CompName = string.Empty;

    /// <summary>
    /// Cached type for the component.
    /// </summary>
    internal Type? Comp;

    /// <summary>
    /// Radius to search around the target entity.
    /// </summary>
    [DataField]
    public float Range = 5f;

    /// <summary>
    /// Flags to use for lookups.
    /// </summary>
    [DataField]
    public LookupFlags Flags = LookupFlags.All;

    /// <summary>
    /// If non-null, found entities must also match this whitelist.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// If non-null, found entities cannot match this blacklist.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// How many entities to search for
    /// </summary>
    [DataField]
    public int MinCount = 1;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => string.Empty;
}

public sealed partial class AnyNearbyConditionSystem : EntityConditionSystem<TransformComponent, AnyNearbyCondition>
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private HashSet<Entity<IComponent>> _found = new();

    protected override void Condition(Entity<TransformComponent> ent, ref EntityConditionEvent<AnyNearbyCondition> args)
    {
        var condition = args.Condition;
        if (condition.Comp == null)
        {
            var reg = Factory.GetRegistration(condition.CompName);
            condition.Comp = reg.Type;
        }
        var type = condition.Comp;

        var range = condition.Range;
        var flags = condition.Flags;
        var whitelist = condition.Whitelist;
        var blacklist = condition.Blacklist;
        var count = condition.MinCount;
        var counter = 0;

        var coords = _transform.GetMapCoordinates(ent, ent.Comp);
        _found.Clear();
        _lookup.GetEntitiesInRange(type, coords, range, _found, flags);
        foreach (var found in _found)
        {
            var uid = found.Owner;
            if (uid == ent.Owner)
                continue;

            if (!_whitelist.CheckBoth(uid, blacklist: blacklist, whitelist: whitelist))
                continue;

            counter++;
        }

        args.Result = counter >= count;
    }
}
