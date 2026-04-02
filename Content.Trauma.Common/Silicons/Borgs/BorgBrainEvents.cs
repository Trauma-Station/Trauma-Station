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

/// <summary>
/// Raised on the chassis after a borg has a MMI/posibrain inserted.
/// </summary>
[ByRefEvent]
public record struct BrainInsertedIntoBorgEvent(EntityUid Brain);

/// <summary>
/// Raised on the chassis after a borg has its MMI/posibrain removed.
/// </summary>
[ByRefEvent]
public record struct BrainRemovedFromBorgEvent(EntityUid Brain);

/// <summary>
/// Raised on the entity used to interact with a borg chassis after the interaction is complete, but before the brain is inserted.
/// </summary>
[ByRefEvent]
public record struct BorgChassisInteractAfterEvent(EntityUid Chassis, EntityUid User, bool Handled = false);

/// <summary>
/// Raised on the Station AI holder when we "try to insert our thing into them" and succeed.
/// </summary>
[ByRefEvent]
public record struct OnIntellicardInsertEvent();
