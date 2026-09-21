// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Factory.Shared.Slots;

namespace Content.Factory.Shared.Machines;

/// <summary>
/// Adds slots to an entity that can be controlled by automation machines.
/// Slots using <see cref="AutomationSlot"/> can provide or accept items.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(AutomationSystem))]
public sealed partial class AutomationSlotsComponent : Component
{
    /// <summary>
    /// All input slots that can be automated.
    /// </summary>
    [DataField(required: true)]
    public List<AutomationSlot> Slots = new();
}
