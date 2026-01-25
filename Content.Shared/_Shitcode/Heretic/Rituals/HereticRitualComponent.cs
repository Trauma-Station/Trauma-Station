using Content.Shared.EntityConditions;
using Content.Shared.Whitelist;

namespace Content.Shared._Shitcode.Heretic.Rituals;

[RegisterComponent]
public sealed partial class HereticRitualComponent : Component
{
    /// <summary>
    /// How many entities ritual can create at once. less or equal than 0 means no limit.
    /// </summary>
    [DataField]
    public int Limit;

    /// <summary>
    /// All entities created by this ritual.
    /// Used for limit check.
    /// </summary>
    [DataField]
    public List<EntityUid> LimitedOutput = new();

    /// <summary>
    /// Events that get raised on the ritual entity
    /// </summary>
    [DataField(required: true), NonSerialized]
    public List<EntityCondition> Conditions = new();

    /// <summary>
    /// Events that are raised if <see cref="Limit"/> has reached <see cref="LimitedOutput"/> count
    /// If this is empty, ritual gets canceled normally
    /// </summary>
    [DataField, NonSerialized]
    public List<EntityCondition> LimitReachedConditions = new();

    /// <summary>
    /// Should this ritual play success animation if <see cref="Events"/> succeeded
    /// </summary>
    [DataField]
    public bool PlaySuccessAnimation = true;

    /// <summary>
    /// Used for events to heretic ritual events to store their results for other methods to use
    /// </summary>
    [DataField, NonSerialized]
    public Dictionary<string, object> Blackboard = new();

    /// <summary>
    /// Loc entry on ritual failure.
    /// May be overriden by ritual events
    /// </summary>
    [DataField]
    public LocId? CancelLoc;
}

[DataDefinition]
public sealed partial class RitualIngredient
{
    [DataField]
    public int Amount = 1;

    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    [DataField(required: true)]
    public LocId Name;
}
