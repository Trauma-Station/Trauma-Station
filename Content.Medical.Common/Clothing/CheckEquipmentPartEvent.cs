// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Medical.Common.Clothing;

/// <summary>
/// Checks if target has missing body part on the slot (due to entier species inventory missing it or part being removed)
/// </summary>
[ByRefEvent]
public record struct CheckEquipmentMissingPartEvent(string Slot, bool Missing = false);
