// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Objectives;

/// <summary>
/// Objective that requires a salvager to claim enough points to meet a quota.
/// </summary>
[RegisterComponent]
public sealed partial class ClaimPointsConditionComponent : Component
{
    /// <summary>
    /// Loc string that has "quota" passed to it.
    /// </summary>
    [DataField(required: true)]
    public LocId Desc;

    /// <summary>
    /// How many points are needed for greentext.
    /// </summary>
    [DataField]
    public int Quota = 5000;
}
