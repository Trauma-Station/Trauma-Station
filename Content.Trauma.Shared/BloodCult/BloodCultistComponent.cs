// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mind;
using Content.Shared.StatusIcon;

namespace Content.Trauma.Shared.BloodCult;

/// <summary>
/// Component added to blood cultists and the leader.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BloodCultistComponent : Component
{
    public override bool SessionSpecific => true;

    [DataField]
    public float HolyConvertTime = 15f;

    [DataField]
    public int MaximumAllowedEmpowers = 4;

    [DataField]
    public Color OriginalEyeColor = Color.White;
}
