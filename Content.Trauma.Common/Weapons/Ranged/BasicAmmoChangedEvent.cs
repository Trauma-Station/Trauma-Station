// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Weapons.Ranged;

/// <summary>
/// Raised on a <c>BasicEntityAmmoProvider</c> when its count is changed.
/// </summary>
[ByRefEvent]
public record struct BasicAmmoChangedEvent(int? Count);
