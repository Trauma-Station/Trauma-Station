// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Chaplain.Components;

/// <summary>
/// Component applied to an entity (usually evil/unholy) to trigger various bad effects inflicted by nullification.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class NullificationComponent : Component
{
    /// <summary>
    /// The nullification this entity currently has.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CurrentNullification;

    /// <summary>
    /// The maximum amount of nullification this entity can have.
    /// </summary>
    [DataField]
    public int MaxNullification = 120;
}
