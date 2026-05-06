// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Cosmos;
using Content.Trauma.Shared.Teleportation;

namespace Content.Trauma.Shared.Heretic.EntityEffects;

public sealed partial class TriggerCosmicMark : EntityEffectBase<TriggerCosmicMark>;

public sealed class TriggerCosmicMarkEffectSystem : EntityEffectSystem<HereticCosmicMarkComponent, TriggerCosmicMark>
{
    [Dependency] private readonly SharedStarMarkSystem _starMark = default!;
    [Dependency] private readonly TeleportSystem _teleport = default!;

    protected override void Effect(Entity<HereticCosmicMarkComponent> ent,
        ref EntityEffectEvent<TriggerCosmicMark> args)
    {
        var targetCoords = Transform(ent).Coordinates;
        _starMark.SpawnCosmicField(targetCoords, ent.Comp.PassiveLevel, predicted: false);

        if (ent.Comp.CosmicDiamondUid is not {} dest || TerminatingOrDeleted(dest))
            return;

        PredictedSpawnAtPosition(ent.Comp.CosmicCloud, targetCoords);
        var newCoords = Transform(dest).Coordinates;
        _teleport.Teleport(ent.Owner, newCoords, user: args.User);
        PredictedSpawnAtPosition(ent.Comp.CosmicCloud, newCoords);
        PredictedDel(ent.Comp.CosmicDiamondUid.Value); // Just in case
    }
}
