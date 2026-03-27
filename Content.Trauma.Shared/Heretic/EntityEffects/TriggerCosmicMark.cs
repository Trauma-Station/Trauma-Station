// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Movement.Pulling.Systems;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Cosmos;

namespace Content.Trauma.Shared.Heretic.EntityEffects;

public sealed partial class TriggerCosmicMark : EntityEffectBase<TriggerCosmicMark>;

public sealed class TriggerCosmicMarkEffectSystem : EntityEffectSystem<HereticCosmicMarkComponent, TriggerCosmicMark>
{
    [Dependency] private readonly SharedStarMarkSystem _starMark = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<HereticCosmicMarkComponent> ent,
        ref EntityEffectEvent<TriggerCosmicMark> args)
    {
        var targetCoords = Transform(ent).Coordinates;
        _starMark.SpawnCosmicField(targetCoords, ent.Comp.PathStage, predicted: false);

        if (!Exists(ent.Comp.CosmicDiamondUid))
            return;

        PredictedSpawnAtPosition(ent.Comp.CosmicCloud, targetCoords);
        var newCoords = Transform(ent.Comp.CosmicDiamondUid.Value).Coordinates;
        _pulling.StopAllPulls(ent);
        _transform.SetCoordinates(ent, newCoords);
        PredictedSpawnAtPosition(ent.Comp.CosmicCloud, newCoords);
        PredictedDel(ent.Comp.CosmicDiamondUid.Value); // Just in case
    }
}
