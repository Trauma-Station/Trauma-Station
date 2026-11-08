// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Containers;

namespace Content.Trauma.Common.Clothing;

/// <summary>
/// AttachedClothingComponent but for MultiToggleableClothingComponent
/// Supports overslotting on top of existing clothing (storing them in side a container)
/// Exists in common module because some upstream code just hardcodes checks for AttachedClothingComponent
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MultiAttachedClothingComponent : Component
{
    public const string DefaultClothingContainerId = "replaced-clothing";

    /// <summary>
    ///     The Id of the piece of clothing that this entity belongs to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid AttachedUid;

    /// <summary>
    ///     Container ID for clothing that will be replaced with this one
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ClothingContainerId = DefaultClothingContainerId;

    [ViewVariables]
    public ContainerSlot ClothingContainer = default!;
}
