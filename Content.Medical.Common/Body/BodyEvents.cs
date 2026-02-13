// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Medical.Common.Body;

/// <summary>
/// Event raised on the body then organ to allow prevention of insertion.
/// Not raised when inserted directly via container API, e.g. ContainerFill.
/// </summary>
[ByRefEvent]
public record struct OrganInsertAttemptEvent(EntityUid Body, EntityUid Organ, bool Cancelled = false);

/// <summary>
/// Event raised on the body then organ to allow prevention of removal.
/// Not raised when removed directly via container API, e.g. entity deletion.
/// </summary>
[ByRefEvent]
public record struct OrganRemoveAttemptEvent(EntityUid Body, EntityUid Organ, bool Cancelled = false);

/// <summary>
/// Event relayed to organs after the mob's eyes are damaged.
/// </summary>
[ByRefEvent]
public record struct EyesDamagedEvent(EntityUid Body, int Damage);

/// <summary>
/// Event raised on a mob to decapitate it.
/// </summary>
[ByRefEvent]
public record struct DecapitateEvent(EntityUid? User = null, bool Handled = true);
