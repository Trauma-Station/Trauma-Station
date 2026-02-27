using Content.Shared._DV.CosmicCult;
using Content.Shared._DV.CosmicCult.Abilities;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared.Alert;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Client._DV.CosmicCult.Abilities;

public sealed partial class CosmicSiphonSystem : SharedCosmicSiphonSystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly CosmicCultSystem _cosmicCult = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize() =>
        base.Initialize();

    protected override void OnCosmicSiphonDoAfter(Entity<CosmicCultComponent> ent, ref EventCosmicSiphonDoAfter args)
    {
        if (args.Args.Target is not { } target
        || args.Cancelled
        || args.Handled
        || !_timing.IsFirstTimePredicted) return;

        base.OnCosmicSiphonDoAfter(ent, ref args);

        RaiseLocalEvent(target, new CosmicSiphonIndicatorEvent());
    }
}
