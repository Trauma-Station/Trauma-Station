// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Flesh;

[RegisterComponent, NetworkedComponent]
public sealed partial class FleshSurgeryComponent : Component
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    [DataField]
    public float AreaHealRange = 7f;

    [DataField]
    public EntProtoId KnitFleshEffect = "KnitFleshEffect";

    [DataField]
    public TimeSpan MaxAreaCooldown = TimeSpan.FromSeconds(40);
}
