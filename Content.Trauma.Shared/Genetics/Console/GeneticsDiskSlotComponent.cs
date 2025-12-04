// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Genetics.Console;

[RegisterComponent, NetworkedComponent, Access(typeof(GeneticsDiskSystem))]
public sealed partial class GeneticsDiskSlotComponent : Component
{
    /// <summary>
    /// Name of the item slot that holds a genetics disk.
    /// </summary>
    [DataField]
    public string DiskSlot = "genetics_disk";
}
