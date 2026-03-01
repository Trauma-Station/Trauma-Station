using Content.Shared.Polymorph.Systems;
using Content.Shared.Popups;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared._DV.CosmicCult.Components.Examine;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Polymorph;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._DV.CosmicCult.Abilities;

public sealed class CosmicLapseSystem : EntitySystem
{
    [Dependency] private readonly SharedCosmicCultSystem _cult = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedPolymorphSystem _polymorph = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;

    private static readonly ProtoId<PolymorphPrototype> HumanLapse = "CosmicLapseMobHuman";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultComponent, EventCosmicLapse>(OnCosmicLapse);
    }

    private void OnCosmicLapse(Entity<CosmicCultComponent> ent, ref EventCosmicLapse action)
    {
        if (action.Handled
            || HasComp<CosmicBlankComponent>(action.Target))
        {
            _popup.PopupClient(Loc.GetString("cosmicability-generic-fail"), ent, ent);
            return;
        }

        var evt = new CosmicAbilityAttemptEvent(action.Target, PlayEffects: true);
        RaiseLocalEvent(ref evt);
        if (evt.Cancelled) return;

        action.Handled = true;
        var tgtpos = Transform(action.Target).Coordinates;
        if (_net.IsServer) // Predicted spawn looks bad with animations
            PredictedSpawnAtPosition(ent.Comp.LapseVFX, tgtpos);

        _popup.PopupClient(Loc.GetString("cosmicability-lapse-success",
            ("target", Identity.Entity(action.Target, EntityManager))),
            ent,
            ent);
        var species = Comp<HumanoidProfileComponent>(action.Target).Species;
        ProtoId<PolymorphPrototype> polymorphId = "CosmicLapseMob" + species;
        if (!_prototype.HasIndex(polymorphId))
            polymorphId = HumanLapse;
        if (!_prototype.Resolve(polymorphId, out var polymorph)) return;
        var copy = polymorph.Configuration;

        if (_cult.EntityIsCultist(action.Target))
        {
            copy.Duration *= 2;
            copy.Forced = false;
        }

        _polymorph.PolymorphEntity(action.Target, copy);

        // Doesn't make an echo because the morph is invisible
    }
}
