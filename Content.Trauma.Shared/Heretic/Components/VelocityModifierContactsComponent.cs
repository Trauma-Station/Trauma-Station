// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VelocityModifierContactsComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Modifier = 1.0f;

    [DataField, AutoNetworkedField]
    public bool IsActive = true;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
}

[NetworkedComponent, RegisterComponent, AutoGenerateComponentState]
public sealed partial class VelocityModifiedByContactComponent : Component
{
    [DataField, AutoNetworkedField]
    public Vector2? OriginalVelocity;
}
