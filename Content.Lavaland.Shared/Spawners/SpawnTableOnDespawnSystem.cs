using Content.Shared.EntityTable;
using Robust.Shared.Spawners;

namespace Content.Lavaland.Shared.Spawners;

public sealed class SpawnTableOnDespawnSystem : EntitySystem
{
    [Dependency] private readonly EntityTableSystem _table = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnTableOnDespawnComponent, TimedDespawnEvent>(OnDespawn);
    }

    private void OnDespawn(EntityUid uid, SpawnTableOnDespawnComponent comp, ref TimedDespawnEvent args)
    {
        var coords = Transform(uid).Coordinates;
        var picked = _table.GetSpawns(comp.Table);
        foreach (var pick in picked)
        {
            Spawn(pick, coords);
        }
    }
}
