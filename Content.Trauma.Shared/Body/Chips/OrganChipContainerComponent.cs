// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Body.Chips;

/// <summary>
/// For organs that can contain some chips with <see cref="OrganChipComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class OrganChipContainerComponent : Component
{
    /// <summary>
    /// Maximum number of chips that can be installed.
    /// </summary>
    [DataField(required: true)]
    public int Limit;

    [DataField]
    public string ContainerName = "organ_chips";

    [ViewVariables]
    public Container Container = default!;
}
