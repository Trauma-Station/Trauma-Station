// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Footprints;

/// <summary>
/// Causes this mob to leave footprint decals after moving/crawling over puddles.
/// </summary>
[RegisterComponent]
public sealed partial class FootprintOwnerComponent : Component
{
    [DataField]
    public float FootDistance = 0.5f;

    [DataField]
    public float BodyDistance = 1;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Distance;

    [DataField]
    public float NextFootOffset = 0.0625f;

    [DataField]
    public int Steps;

    [DataField]
    public Color Color;
}
