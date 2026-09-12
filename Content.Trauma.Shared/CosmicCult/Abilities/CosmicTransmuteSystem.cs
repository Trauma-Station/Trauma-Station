// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.CosmicCult.Components;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.CosmicCult.Abilities;

public sealed partial class CosmicTransmuteSystem : EntitySystem
{
    [Dependency] private SharedCosmicCultSystem _cult = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedHandsSystem _hand = default!;
    [Dependency] private PullingSystem _pull = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private EntityQuery<CosmicTransmutableComponent> _query = default!;

    [SubscribeLocalEvent]
    private void OnDetailedExamine(Entity<CosmicTransmutableComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        // non-cultists don't need to know this
        if (!_cult.EntityIsCultist(args.User) || !ProtoMan.TryIndex(ent.Comp.TransmuteTo, out var result))
            return;

        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString("cosmic-examine-transmutable", ("result", result.Name)));
        if (ent.Comp.RequiresEmpowerment)
        {
            msg.PushNewline();
            msg.AddMarkupOrThrow(Loc.GetString("cosmic-examine-transmutable-empowerment"));
        }
        _examine.AddHoverExamineVerb(args,
            ent.Comp,
            Loc.GetString("cosmic-examine-transmutable-verb-text"),
            msg.ToMarkup(),
            "/Textures/_DV/CosmicCult/Interface/transmute_inspect.png");
    }

    // Contrary to popular belief, does not make you a mute trans person
    [SubscribeLocalEvent]
    private void OnTransmute(Entity<CosmicCultComponent> ent, ref CosmicTransmutationEvent args)
    {
        if (args.Handled)
            return;

        if (!TryTransmutePulled(ent, out var message) && !TryTransmuteHeld(ent, out message)) // That's some slightly less wonky code layout here
        {
            _popup.PopupEntity(Loc.GetString(message ?? "cosmicability-transmute-no-item"), ent, ent);
            return;
        }

        args.Handled = true;
        _audio.PlayPredicted(ent.Comp.TransmuteSFX, ent, ent);
        if (_net.IsServer) // Predicted spawn looks bad with animations
            PredictedSpawnAtPosition(ent.Comp.TransmuteVFX, Transform(ent).Coordinates);
    }

    private bool TryTransmutePulled(Entity<CosmicCultComponent> ent, out LocId? message)
    {
        message = null;
        if (_pull.GetPulling(ent.Owner) is not { } target)
            return false;

        if (!_query.TryComp(target, out var comp) ||
            !ProtoMan.TryIndex(comp.TransmuteTo, out var proto))
        {
            message = "cosmicability-transmute-not-transmutable";
            return false;
        }
        if (comp.RequiresEmpowerment && !ent.Comp.CosmicEmpowered)
        {
            message = "cosmicability-transmute-not-empowered";
            return false;
        }

        var tgtPos = Transform(target).Coordinates;
        var result = PredictedSpawnAtPosition(proto.ID, tgtPos);
        _pull.TryStartPull(ent, result);
        PredictedQueueDel(target);
        return true;
    }

    private bool TryTransmuteHeld(Entity<CosmicCultComponent> ent, out LocId? message)
    {
        message = null;
        if (!_hand.TryGetActiveItem(ent.Owner, out var item) || item is not { } target)
            return false;

        if (!_query.TryComp(target, out var comp) ||
            !ProtoMan.TryIndex(comp.TransmuteTo, out var proto))
        {
            message = "cosmicability-transmute-not-transmutable";
            return false;
        }

        if (comp.RequiresEmpowerment && !ent.Comp.CosmicEmpowered)
        {
            message = "cosmicability-transmute-not-empowered";
            return false;
        }

        var result = PredictedSpawnAtPosition(proto.ID, Transform(ent).Coordinates);
        _hand.TryDrop(ent.Owner);
        _hand.TryForcePickupAnyHand(ent.Owner, result);
        PredictedQueueDel(target);
        return true;
    }
}
