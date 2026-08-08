// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LaserBeamEndpointComponent : Component
{
    [DataField]
    public bool PvsOverride = true;

    [DataField, AutoNetworkedField]
    public EntityUid? Gun;
}
