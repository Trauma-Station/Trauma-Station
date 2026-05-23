// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.MartialArts.Components;

/// <summary>
/// Added to the target after using armbar move.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ArmbarredComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid User;

    /// <summary>
    /// Armbar breaks if the distance to the user exceeds this distance.
    /// </summary>
    [DataField]
    public float Range = 2f;
}
