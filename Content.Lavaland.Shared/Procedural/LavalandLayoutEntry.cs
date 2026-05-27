// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Lavaland.Shared.Procedural;

[DataRecord]
public partial record struct LavalandLayoutEntry(
    ResPath GridPath,
    Vector2 Position,
    LocId Name);
