// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.CosmicCult.Components;
using Content.Trauma.Shared.CosmicCult.Components.Examine;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Polymorph;
using Content.Shared.Polymorph.Systems;
using Content.Shared.Popups;

namespace Content.Trauma.Shared.CosmicCult.Abilities;

public sealed partial class CosmicLapseSystem : EntitySystem
{
    [Dependency] private SharedCosmicCultSystem _cult = default!;
    [Dependency] private SharedPolymorphSystem _polymorph = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private INetManager _net = default!;

    private static readonly ProtoId<PolymorphPrototype> HumanLapse = "CosmicLapseMobHuman";

    [SubscribeLocalEvent]
    private void OnCosmicLapse(Entity<CosmicCultComponent> ent, ref CosmicLapseEvent args)
    {
        if (args.Handled || HasComp<CosmicBlankComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("cosmicability-generic-fail"), ent, ent);
            return;
        }

        var evt = new CosmicAbilityAttemptEvent(args.Target, PlayEffects: true);
        RaiseLocalEvent(ref evt);
        if (evt.Cancelled)
            return;

        args.Handled = true;
        var tgtpos = Transform(args.Target).Coordinates;
        if (_net.IsServer) // Predicted spawn looks bad with animations
            PredictedSpawnAtPosition(ent.Comp.LapseVFX, tgtpos);

        _popup.PopupEntity(Loc.GetString("cosmicability-lapse-success",
            ("target", Identity.Entity(args.Target, EntityManager))),
            ent,
            ent);
        var species = Comp<HumanoidProfileComponent>(args.Target).Species;
        ProtoId<PolymorphPrototype> polymorphId = "CosmicLapseMob" + species;
        if (!ProtoMan.HasIndex(polymorphId))
            polymorphId = HumanLapse;
        if (!ProtoMan.Resolve(polymorphId, out var polymorph))
            return;

        var copy = polymorph.Configuration;
        if (_cult.EntityIsCultist(args.Target))
        {
            copy.Duration *= 2;
            copy.Forced = false;
        }

        _polymorph.PolymorphEntity(args.Target, copy);

        // Doesn't make an echo because the morph is invisible
    }
}
