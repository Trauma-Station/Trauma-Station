// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Lavaland.Shared.Procedural.Prototypes;

[Prototype]
public sealed partial class LavalandRuinPoolPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    /// Distance in-between ruins.
    /// </summary>
    [DataField]
    public int RuinDistance = 16;

    /// <summary>
    /// Max distance that Ruins can generate.
    /// </summary>
    [DataField]
    public int MaxDistance = 256; // TODO make this value higher after proper GPS UI is added

    /// <summary>
    /// List of all grid ruins and their count.
    /// Used for ruins that are loaded as proper grids.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<LavalandGridRuinPrototype>, ushort> GridRuins = new();

    /// <summary>
    /// List of all dungeon ruins and their count.
    /// Used for ruins that are generated with Dungeon markers.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<LavalandDungeonRuinPrototype>, ushort> DungeonRuins = new();
}
