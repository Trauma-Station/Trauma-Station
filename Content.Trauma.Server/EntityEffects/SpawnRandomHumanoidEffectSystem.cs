// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Humanoid.Systems;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.EntityEffects;

namespace Content.Trauma.Server.EntityEffects;

public sealed partial class SpawnRandomHumanoidEntityEffectSystem : EntityEffectSystem<TransformComponent, SpawnRandomHumanoid>
{
    [Dependency] private RandomHumanoidSystem _randomHumanoid = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<SpawnRandomHumanoid> args)
    {
        _randomHumanoid.SpawnRandomHumanoid(args.Effect.Settings, entity.Comp.Coordinates, MetaData(entity).EntityName);
    }
}
