// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Blade;

[RegisterComponent, NetworkedComponent]
public sealed partial class SilverMaelstromComponent : Component
{
    public override bool SessionSpecific => true;

    [DataField]
    public EntProtoId Status = "SilverMaelstromStatusEffect";

    [DataField]
    public float ExtraDamageMultiplier = 0.5f;

    [DataField]
    public float LifestealHealMultiplier = 0.25f;
}
