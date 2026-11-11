// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.LinkedEntityContainer;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Spawns linked entity. Used with `LinkedEntityContainerComponent`.
/// </summary>
public sealed partial class SpawnLinkedEntity : EntityEffectBase<SpawnLinkedEntity>
{
    /// <summary>
    /// Spawns one way portal.
    /// </summary>
    [DataField]
    public EntProtoId LinkedEnt = "BluespaceRiftPortalDestination";
}

public sealed partial class SpawnLinkedEntityEffectSystem : EntityEffectSystem<LinkedEntityContainerComponent, SpawnLinkedEntity>
{
    protected override void Effect(Entity<LinkedEntityContainerComponent> ent, ref EntityEffectEvent<SpawnLinkedEntity> args)
    {
        var coords = Transform(ent).Coordinates;
        var newEnt = PredictedSpawnAtPosition(args.Effect.LinkedEnt, coords);
        ent.Comp.Uid = newEnt;
        Dirty(ent);
    }
}
