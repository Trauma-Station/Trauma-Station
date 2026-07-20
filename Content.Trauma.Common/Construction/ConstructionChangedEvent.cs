// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Construction;

/// <summary>
/// Raised on the original entity after spawning a replacement entity before it gets deleted.
/// E.g. using shiv on unfinished bat, changed event is raised on the unfinished bat with Target as the new bat.
/// </summary>
/// <remarks>
/// only exists here because idiots put the event in server 4 no raisin
/// </remarks>
[ByRefEvent]
public record struct ConstructionChangedEvent(EntityUid Target);
