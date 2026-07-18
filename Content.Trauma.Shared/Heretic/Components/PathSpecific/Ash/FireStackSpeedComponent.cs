// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Ash;

[RegisterComponent, NetworkedComponent]
public sealed partial class FireStackSpeedComponent : Component
{
    [DataField]
    public float FireStackSpeedMultiplier = 0.02f;
}
