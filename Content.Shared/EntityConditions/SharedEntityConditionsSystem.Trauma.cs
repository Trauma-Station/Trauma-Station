// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.EntityConditions;

/// <summary>
/// Trauma - reporting which condition failed, for popups.
/// </summary>
public sealed partial class SharedEntityConditionsSystem
{
    /// <summary>
    /// Checks a list of conditions to verify that they all return true, and reports the one that failed.
    /// </summary>
    public bool TryConditions<T>(EntityUid target, T[]? conditions, out T? failed, EntityUid? sourceEnt = null)
        where T : EntityCondition
    {
        failed = null;
        if (conditions == null)
            return true;

        foreach (var condition in conditions)
        {
            if (TryCondition(target, condition, sourceEnt))
                continue;

            failed = condition;
            return false;
        }

        return true;
    }
}

public abstract partial class EntityCondition
{
    /// <summary>
    /// Optional popup to show when this condition fails.
    /// Only used by systems that check conditions one at a time, like combos.
    /// </summary>
    [DataField]
    public LocId? FailMessage;
}
