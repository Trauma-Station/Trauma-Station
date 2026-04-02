// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Forging;

/// <summary>
/// Component for items that are fully made out of 1 metal with uniform properties.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedMetalSystem))]
[AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class MetallicComponent : Component
{
    /// <summary>
    /// Temperature at which the metal stops being soft enough to work on.
    /// If this is 0 they will be updated to the metal's default when it gets changed.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float MinTemp;

    /// <summary>
    /// Ideal temperature for working on this metal.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float IdealTemp;

    /// <summary>
    /// The metal prototype to use properties of.
    /// If this is null it must be changed right after it is spawned.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<MetalPrototype>? Metal;

    /// <summary>
    /// Once outside temperature is this hot, you will take damage from holding it.
    /// </summary>
    [DataField]
    public float DamageHoldingTemp = 353.15f;

    /// <summary>
    /// Whether it is currently workable.
    /// Set when it is heated up to <see cref="IdealTemp"/>, unset when cooled below <see cref="MinTemp"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Workable;
}

/// <summary>
/// Raised on a metallic item when <c>Workable</c> changes.
/// </summary>
[ByRefEvent]
public record struct MetalWorkableChangedEvent(bool Workable);

/// <summary>
/// Raised on a metallic item when <c>Metal</c> changes.
/// This is done for procedurally generated forged items.
/// </summary>
[ByRefEvent]
public record struct MetalChangedEvent(MetalPrototype Metal);

[Serializable, NetSerializable]
public enum MetallicVisuals : byte
{
    Layer
}
