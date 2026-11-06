using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Nest;

/// <summary>
/// Component that triggers effects when <see cref="CurrentPoints"/> reaches <see cref="TriggerPoints"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class NestComponent : Component
{
    /// <summary>
    /// The points required to trigger the effects
    /// </summary>
    [DataField]
    public int TriggerPoints = 2;

    /// <summary>
    /// How many points to adjust <see cref="CurrentPoints"/> by.
    /// </summary>
    [DataField]
    public int PointAdjust = 1;

    /// <summary>
    /// The current points of the nest.
    /// The points increase when an entity is inserted into <see cref="ContainerId"/>'s container.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CurrentPoints;

    /// <summary>
    /// What entities are allowed to be inserted into the nest
    /// </summary>
    [DataField]
    public EntityCondition[]? Conditions;

    /// <summary>
    /// The id of the container
    /// </summary>
    [DataField(required: true)]
    public string ContainerId;

    /// <summary>
    /// Effects to run when <see cref="CurrentPoints"/> reaches <see cref="TriggerPoints"/>.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    /// <summary>
    /// Effects to run when the user gets inserted into the nest
    /// </summary>
    [DataField]
    public EntityEffect[]? TargetEffects;
}
