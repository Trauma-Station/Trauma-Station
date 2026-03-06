// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Silicons.Borgs;

/// <summary>
/// Raised on the MMI/posibrain (and relayed to the brain inside) after a borg has a brain inserted.
/// </summary>
[ByRefEvent]
public record struct BorgBrainInsertedEvent(EntityUid Chassis, EntityUid Brain);

/// <summary>
/// Raised on the MMI/posibrain (and relayed to the brain inside) after a borg has a brain removed.
/// </summary>
[ByRefEvent]
public record struct BorgBrainRemovedEvent(EntityUid Chassis, EntityUid Brain);
