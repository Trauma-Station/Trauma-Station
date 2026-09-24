// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BloodCult.Components;

/// <summary>
/// Added to all cultists when the cult reaches CultStage.Pentagram.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PentagramComponent : Component
{
    [DataField]
    public ResPath RsiPath = new("/Textures/_Trauma/BloodCult/Effects/pentagram.rsi");

    [DataField]
    public string[] States =
    [
        "halo1",
        "halo2",
        "halo3",
        "halo4",
        "halo5",
        "halo6"
    ];
}
