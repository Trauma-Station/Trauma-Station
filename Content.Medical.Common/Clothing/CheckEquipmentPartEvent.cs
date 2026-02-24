// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Medical.Common.Clothing;

/// <summary>
/// Used by modsuits to check if an item can be equipped to a slot on the event target.
/// Set <c>Handled</c> to true if it's allowed.
/// </summary>
[ByRefEvent]
public record struct CheckEquipmentPartEvent(string Slot, bool Handled = false);
