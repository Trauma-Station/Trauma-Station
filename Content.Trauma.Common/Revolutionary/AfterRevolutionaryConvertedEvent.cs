// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Revolutionary;

/// <summary>
/// Called after a converter is used on another person to check for rev conversion.
/// Raised on the user of the converter, the target hit by the converter, and the converter used.
/// </summary>
[ByRefEvent]
public readonly struct AfterRevolutionaryConvertedEvent(EntityUid target, EntityUid? user, EntityUid? used)
{
    public readonly EntityUid Target = target;
    public readonly EntityUid? User = user;
    public readonly EntityUid? Used = used;
}
