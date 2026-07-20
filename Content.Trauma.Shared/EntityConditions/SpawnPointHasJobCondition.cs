// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Condition that requires the target player spawn point has a non-null <c>Job</c> set.
/// </summary>
public sealed partial class SpawnPointHasJob : EntityConditionBase<SpawnPointHasJob>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => string.Empty;
}
