// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Containers;

/// <summary>
/// Effects that run on the entity that got inserted into the container that owns this component.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EffectsOnContainerComponent : Component
{
    /// <summary>
    /// Effects to run when getting inserted.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Inserted = default!;

    /// <summary>
    /// Effects to run when getting removed.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Removed = default!;

    /// <summary>
    /// The container the entity has to be inserted into to run the effects.
    /// Null will allow all containers.
    /// </summary>
    [DataField]
    public string? ContainerId;
}
