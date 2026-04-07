// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Heretic.Components.Side.Carvings;

[RegisterComponent, NetworkedComponent]
public sealed partial class MadCarvingComponent : Component
{
    [DataField]
    public float StaminaDamage = 80f;

    [DataField]
    public TimeSpan MuteTime = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan BlindnessTime = TimeSpan.FromSeconds(10);
}
