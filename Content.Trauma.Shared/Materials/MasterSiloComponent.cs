// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Materials;
using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Materials;

/// <summary>
/// Distributes inserted materials between all powered material silos on the same grid.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MasterSiloComponent : Component
{
    /// <summary>
    /// Whitelist materials must match to be distributed.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Blacklist materials cannot match to be distributed.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Accumulated remainders of dividing material amount between silos, used to prevent deleting small amounts of materials in the long run.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<MaterialPrototype>, int> Leftovers = new();
}
