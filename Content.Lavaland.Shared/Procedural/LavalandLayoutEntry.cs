using System.Numerics;
using Robust.Shared.Utility;

namespace Content.Lavaland.Shared.Procedural;

[DataRecord]
public partial record struct LavalandLayoutEntry(
    ResPath GridPath,
    Vector2 Position,
    LocId Name);
