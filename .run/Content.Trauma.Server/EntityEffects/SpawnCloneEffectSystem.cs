// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Cloning;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Trauma.Shared.EntityEffects;

namespace Content.Trauma.Server.EntityEffects;

public sealed partial class SpawnCloneEffectSystem : EntityEffectSystem<HumanoidProfileComponent, SpawnClone>
{
    [Dependency] private CloningSystem _cloning = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<HumanoidProfileComponent> ent, ref EntityEffectEvent<SpawnClone> args)
    {
        var effect = args.Effect;

        var settings = effect.Settings;
        var mapCoords = _transform.GetMapCoordinates(ent);

        if (!_cloning.TryCloning(ent.Owner, mapCoords, settings, out var clone) || clone is not { } cloneEnt)
            return;

        if (effect.ComponentsToRemove is { } componentsToRemove)
            EntityManager.RemoveComponents(cloneEnt, componentsToRemove);

        if (effect.ComponentsToAdd is { } componentsToAdd)
            EntityManager.AddComponents(cloneEnt, componentsToAdd);
    }
}
