// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Lavaland.Common.Procedural;
using Content.Lavaland.Server.Procedural;
using Content.Shared.Parallax.Biomes;
using Robust.Shared.Map.Enumerators;

namespace Content.Lavaland.Server.Biome;

/// <summary>
/// System that stores already loaded chunks and stops BiomeSystem from unloading them.
/// This should finally prevent server from fucking dying because of 80 players on lavaland at the same time
/// </summary>
public sealed class LavalandMapOptimizationSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BiomeOptimizeComponent, ChunkLoadAttemptEvent>(OnChunkLoadAttempt);
    }

    private void OnChunkLoadAttempt(Entity<BiomeOptimizeComponent> ent, ref ChunkLoadAttemptEvent args)
    {
        // We load only specified area around the origin.
        args.Cancelled |= !ent.Comp.LoadArea.Contains(args.Chunk);
    }
}
