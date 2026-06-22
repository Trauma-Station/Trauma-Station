// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Shared.Wraith.Components.Mobs;

[RegisterComponent, NetworkedComponent]
public sealed partial class SummonRatDenComponent : Component
{
    [DataField]
    public EntProtoId RatDen = "RatDen";

    [DataField]
    public EntityUid? RatDenUid;
}
