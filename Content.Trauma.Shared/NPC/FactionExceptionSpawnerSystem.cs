// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Trauma.Common.Spawners;

namespace Content.Trauma.Shared.NPC;

/// <summary>
/// Makes timed spawners with <see cref="FactionExceptionComponent"/> copy ignored mobs to its spawned entities.
/// </summary>
public sealed class FactionExceptionSpawnerSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _faction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FactionExceptionComponent, SpawnerSpawnedEvent>(OnSpawned);
    }

    private void OnSpawned(Entity<FactionExceptionComponent> ent, ref SpawnerSpawnedEvent args)
    {
        _faction.IgnoreEntities(args.Spawned, ent.Comp.Ignored);
    }
}
