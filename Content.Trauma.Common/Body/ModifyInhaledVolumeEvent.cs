// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Common.Body;

/// <summary>
/// Raised on a mob to modify how much of the breathed atmosphere it can inhale.
/// </summary>
[ByRefEvent]
public record struct ModifyInhaledVolumeEvent(float Volume);
