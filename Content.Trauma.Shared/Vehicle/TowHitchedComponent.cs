// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Vehicle;

/// <summary>
/// Component applied to entities that are currently hitched
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class TowHitchedComponent : Component
{
    /// <summary>
    /// The entity we are hitched to
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? HitchedTo;
}
