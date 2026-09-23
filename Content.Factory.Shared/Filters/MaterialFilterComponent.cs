// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Materials;

namespace Content.Factory.Shared.Filters;

/// <summary>
/// Filters entities that have <see cref="MaterialComponent"/> and are made up of a whitelisted material.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(AutomationFilterSystem))]
[AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class MaterialFilterComponent : Component
{
    /// <summary>
    /// Whether to change <see cref="Whitelist"/> to a blacklist and allow non-material entities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Inverted;

    /// <summary>
    /// The materials allowed/denied by the filter, controlled by <see cref="Inverted"/>.
    /// Behaves like a list of every material if it's empty for usability.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<MaterialPrototype>> Whitelist = new();
}

[Serializable, NetSerializable]
public sealed partial class MaterialFilterInvertMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed partial class MaterialFilterToggleMessage(ProtoId<MaterialPrototype> material) : BoundUserInterfaceMessage
{
    public readonly ProtoId<MaterialPrototype> Material = material;
}
