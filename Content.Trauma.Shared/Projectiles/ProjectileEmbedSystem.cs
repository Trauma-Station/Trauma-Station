// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Projectiles;

namespace Content.Trauma.Shared.Projectiles;

public sealed partial class ProjectileEmbedSystem : EntitySystem
{
    [Dependency] private SharedProjectileSystem _projectile = default!;

    // picking up or e.g. tilegun sucking up a tile unembeds it automatically
    [SubscribeLocalEvent]
    private void OnInsertedIntoContainer(Entity<EmbeddableProjectileComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        _projectile.EmbedDetach(ent, ent.Comp);
    }
}
