// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.GameTicking;

/// <summary>
/// Helper method for deciding if a new antag is required
/// </summary>
public abstract class CommonNewAntagOrEvacSystem : EntitySystem
{
    public abstract void SpawnNewAntagIfBelowPercent(EntityUid uid, TimeSpan countDownTime, bool cantRecall, bool endIfUnderPercent = true);
}
