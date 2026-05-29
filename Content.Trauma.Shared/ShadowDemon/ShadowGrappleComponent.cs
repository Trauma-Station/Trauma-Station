// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.ShadowDemon;

/// <summary>
/// Grants you shadow grapple action
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ShadowGrappleComponent : Component
{
    [DataField]
    public EntProtoId ActionId = "ShadowGrappleAction";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionUid;
}
