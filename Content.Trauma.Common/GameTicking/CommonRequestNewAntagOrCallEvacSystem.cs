// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.GameTicking;

/// <summary>
/// Helper method for deciding if a new antag is required
/// </summary>
public abstract class CommonRequestNewAntagOrCallEvacSystem : EntitySystem
{
    public abstract void SpawnNewAntagIfBelowPercent(float percent, int aliveOnSpawn, TimeSpan countDownTime, EntProtoId antagsToSpawn, bool cantRecall, bool endIfUnderPercent = true);
}
