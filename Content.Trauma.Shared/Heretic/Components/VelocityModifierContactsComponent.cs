// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VelocityModifierContactsComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string CollisionFixture;

    [DataField, AutoNetworkedField]
    public float Modifier = 1.0f;

    [DataField, AutoNetworkedField]
    public bool IsActive = true;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;
}

[NetworkedComponent, RegisterComponent, AutoGenerateComponentState]
public sealed partial class VelocityModifiedByContactComponent : Component
{
    [DataField, AutoNetworkedField]
    public Vector2? OriginalVelocity;
}
