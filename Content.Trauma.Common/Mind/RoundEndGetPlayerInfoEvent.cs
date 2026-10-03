// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;

namespace Content.Trauma.Common.Mind;

/// <summary>
/// Raised on a mind to get extra player info for the round end summary.
/// </summary>
[ByRefEvent]
public record struct RoundEndGetPlayerInfoEvent()
{
    public string? LastWords;
    public byte MobState; // W common
    public Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> DamagePerGroup = new();
}
