// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Weapons.Ranged;

/// <summary>
/// Changes the item's HeldPrefix depending on being loaded or empty.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BasicAmmoHeldVisualsComponent : Component
{
    [DataField]
    public string? EmptyPrefix;

    [DataField]
    public string? LoadedPrefix = "loaded";
}
