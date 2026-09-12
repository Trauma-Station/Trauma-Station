// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.AER;

/// <summary>
/// event raised for updating the active status of a AER
/// </summary>
[ByRefEvent]
public record struct AerUpdateActiveStatusEvent(EntityUid Aer, bool Active);
