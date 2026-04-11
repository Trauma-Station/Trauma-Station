// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Random;

namespace Content.Goobstation.Server.RandomChanceSpawner;

public sealed partial class RandomChanceSpawnerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomChanceSpawnerComponent, MapInitEvent>(OnMapInit);
    }

    public void OnMapInit(Entity<RandomChanceSpawnerComponent> ent, ref MapInitEvent args)
    {
        foreach (var (id, chance) in ent.Comp.ToSpawn)
        {
            if (_random.Prob(chance))
                Spawn(id, Transform(ent).Coordinates);
        }
        QueueDel(ent);
    }
}
