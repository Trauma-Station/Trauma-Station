using Content.Server.Ghost;
using Content.Shared._DV.CosmicCult;
using Content.Shared._DV.CosmicCult.Abilities;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared.Alert;
using Content.Shared.Light.Components;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server._DV.CosmicCult.Abilities;

public sealed partial class CosmicSiphonSystem : SharedCosmicSiphonSystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly CosmicCultRuleSystem _cultRule = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly HashSet<Entity<PoweredLightComponent>> _lights = [];

    public override void Initialize() =>
        base.Initialize();

    protected override void OnCosmicSiphonDoAfter(Entity<CosmicCultComponent> uid, ref EventCosmicSiphonDoAfter args)
    {
        if (args.Args.Target is not { } target
            || args.Cancelled
            || args.Handled)
            return;
        args.Handled = true;

        var entropySiphoned = Math.Min(uid.Comp.CosmicSiphonQuantity, uid.Comp.EntropyStoredCap - uid.Comp.EntropyStored); // Prevent going over the cap

        uid.Comp.EntropyStored += entropySiphoned;
        uid.Comp.EntropyBudget += entropySiphoned;
        Dirty(uid, uid.Comp);

        _alerts.ShowAlert(uid.Owner, uid.Comp.EntropyAlert);
        _cultRule.IncrementCultObjectiveEntropy(uid);

        if (uid.Comp.CosmicEmpowered) // if you're empowered there's a 20% chance to flicker lights on siphon. Not predicted because GhostSystem isn't (and who cares anyway).
        {
            _lights.Clear();
            _lookup.GetEntitiesInRange<PoweredLightComponent>(Transform(uid).Coordinates, uid.Comp.FlickerRange, _lights, LookupFlags.StaticSundries);
            foreach (var light in _lights) // static range of 5. because.
            {
                if (!_random.Prob(uid.Comp.FlickerProbability))
                    continue;

                _ghost.DoGhostBooEvent(light);
            }
        }
    }
}
