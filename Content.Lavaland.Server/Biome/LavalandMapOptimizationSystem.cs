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

        SubscribeLocalEvent<BiomeOptimizeComponent, ChunkUnloadAttemptEvent>(OnChunkUnloadAttempt);
        SubscribeLocalEvent<BiomeOptimizeComponent, ChunkLoadAttemptEvent>(OnChunkLoadAttempt);
        SubscribeLocalEvent<BiomeOptimizeComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<BiomeOptimizeComponent> ent, ref MapInitEvent args)
    {
        var enumerator = new ChunkIndicesEnumerator(ent.Comp.LoadArea, SharedBiomeSystem.ChunkSize);

        while (enumerator.MoveNext(out var chunk))
        {
            var chunkOrigin = chunk * SharedBiomeSystem.ChunkSize;
            ent.Comp.LoadedChunks.Add(chunkOrigin.Value);
        }
    }

    private void OnChunkUnloadAttempt(Entity<BiomeOptimizeComponent> ent, ref ChunkUnloadAttemptEvent args)
    {
        // We don't unload chunks in the preloaded area since it's expensive.
        args.Cancelled |= ent.Comp.LoadedChunks.Contains(args.Chunk);
    }

    private void OnChunkLoadAttempt(Entity<BiomeOptimizeComponent> ent, ref ChunkLoadAttemptEvent args)
    {
        // We load only specified area around the origin.
        args.Cancelled |= !ent.Comp.LoadArea.Contains(args.Chunk);
    }
}
