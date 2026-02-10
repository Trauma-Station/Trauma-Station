// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Common.Kitchen;

/// <summary>
/// Raised on the mob being butchered to allow cancelling it.
/// </summary>
[ByRefEvent]
public record struct ButcherAttemptEvent(LocId? CancelPopup = null);
