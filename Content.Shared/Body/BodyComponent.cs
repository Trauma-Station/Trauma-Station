using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Body;

[RegisterComponent, NetworkedComponent]
[Access(typeof(BodySystem))]
public sealed partial class BodyComponent : Component
{
    public const string ContainerID = "body_organs";

    /// <summary>
    /// The actual container with entities with <see cref="OrganComponent" /> in it
    /// </summary>
    [ViewVariables]
    public Container? Organs;
}

/// <summary>
/// Raised on organ entity, when it is inserted into a body
/// </summary>
[ByRefEvent]
public readonly record struct OrganGotInsertedEvent(Entity<BodyComponent> Target, OrganComponent Organ); // Trauma - pass an Entity, add OrganComponent

/// <summary>
/// Raised on organ entity, when it is removed from a body
/// </summary>
[ByRefEvent]
public readonly record struct OrganGotRemovedEvent(Entity<BodyComponent> Target, OrganComponent Organ); // Trauma - pass an Entity, add OrganComponent

/// <summary>
/// Raised on body entity, when an organ is inserted into it
/// </summary>
[ByRefEvent]
public readonly record struct OrganInsertedIntoEvent(Entity<OrganComponent> Organ); // Trauma - pass an Entity

/// <summary>
/// Raised on body entity, when an organ is removed from it
/// </summary>
[ByRefEvent]
public readonly record struct OrganRemovedFromEvent(Entity<OrganComponent> Organ); // Trauma - pass an Entity
