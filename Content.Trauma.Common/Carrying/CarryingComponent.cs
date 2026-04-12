// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Common.Carrying;

/// <summary>
/// Added to an entity when they are carrying somebody.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class CarryingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Carried;
}
