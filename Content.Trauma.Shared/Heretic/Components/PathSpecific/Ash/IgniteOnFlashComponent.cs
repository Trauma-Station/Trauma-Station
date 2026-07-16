// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Ash;

[RegisterComponent, NetworkedComponent]
public sealed partial class IgniteOnFlashComponent : Component
{
    [DataField]
    public float FireStacks;

    [DataField]
    public float FireProtectionPenetration;
}
