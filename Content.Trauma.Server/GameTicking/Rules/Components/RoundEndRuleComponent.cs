// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.GameTicking.Rules.Components;

/// <summary>
/// Requests round end when started
/// </summary>
[RegisterComponent]
public sealed partial class RoundEndRuleComponent : Component
{
    [DataField]
    public TimeSpan CountdownTime = TimeSpan.FromMinutes(10);

    [DataField]
    public bool CheckCooldown = false;

    [DataField]
    public bool CantRecall = true;
}
