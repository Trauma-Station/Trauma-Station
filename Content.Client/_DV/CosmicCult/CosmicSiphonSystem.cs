using Content.Shared._DV.CosmicCult;
using Content.Shared._DV.CosmicCult.Abilities;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared.Alert;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Client._DV.CosmicCult.Abilities;

public sealed partial class CosmicSiphonSystem : SharedCosmicSiphonSystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly CosmicCultSystem _cosmicCult = default!;

    public override void Initialize() =>
        base.Initialize();

    protected override void OnCosmicSiphonDoAfter(Entity<CosmicCultComponent> ent, ref EventCosmicSiphonDoAfter args)
    {
        if (args.Args.Target is not { } target
            || args.Cancelled
            || args.Handled
            || !_timing.IsFirstTimePredicted)
            return;
        args.Handled = true;

        RaiseLocalEvent(target, new CosmicSiphonIndicatorEvent());

        _cosmicCult.AddEntropy(ent, ent.Comp.CosmicSiphonQuantity);

        _popup.PopupClient(Loc.GetString("cosmicability-siphon-success", ("target", Identity.Entity(target, EntityManager))), ent, ent);
    }
}
