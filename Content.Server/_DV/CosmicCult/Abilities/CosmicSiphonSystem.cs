using Content.Server.Ghost;
using Content.Shared._DV.CosmicCult;
using Content.Shared._DV.CosmicCult.Abilities;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared.Light.Components;
using Robust.Shared.Random;

namespace Content.Server._DV.CosmicCult.Abilities;

public sealed partial class CosmicSiphonSystem : SharedCosmicSiphonSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private readonly HashSet<Entity<PoweredLightComponent>> _lights = [];

    protected override void OnCosmicSiphonDoAfter(Entity<CosmicCultComponent> ent, ref EventCosmicSiphonDoAfter args)
    {
        if (args.Cancelled
        || args.Handled) return;

        base.OnCosmicSiphonDoAfter(ent, ref args);

        if (ent.Comp.CosmicEmpowered) // if you're empowered there's a 20% chance to flicker lights on siphon. Not predicted because GhostSystem isn't (and who cares anyway).
        {
            _lights.Clear();
            _lookup.GetEntitiesInRange(Transform(ent).Coordinates, ent.Comp.FlickerRange, _lights, LookupFlags.StaticSundries);
            foreach (var light in _lights) // static range of 5. because.
            {
                if (!_random.Prob(ent.Comp.FlickerProbability))
                    continue;

                _ghost.DoGhostBooEvent(light);
            }
        }
    }
}
