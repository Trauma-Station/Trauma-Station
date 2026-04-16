// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;

[RegisterComponent, NetworkedComponent]
public sealed partial class StarTouchedComponent : Component
{
    [DataField]
    public float TickInterval = 0.2f;

    [DataField]
    public float Range = 8f;

    [DataField]
    public bool ApplyEffects;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Accumulator;
}
