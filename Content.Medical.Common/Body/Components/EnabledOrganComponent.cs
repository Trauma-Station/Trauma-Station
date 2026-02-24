// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Medical.Common.Body;

/// <summary>
/// Organ component added while inserted and not explicitly disabled by e.g. damage, cybernetic emps.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EnabledOrganComponent : Component;

/// <summary>
/// Event raised on the organ to allow preventing it being enabled.
/// </summary>
[ByRefEvent]
public record struct OrganEnableAttemptEvent(EntityUid Body, bool Cancelled = false);

// no disable attempt, use your head

/// <summary>
/// Event raised on the organ after it has been enabled.
/// </summary>
[ByRefEvent]
public readonly record struct OrganEnabledEvent(EntityUid Body);

/// <summary>
/// Event raised on the organ after it has been disabled.
/// </summary>
[ByRefEvent]
public readonly record struct OrganDisabledEvent(EntityUid Body);
