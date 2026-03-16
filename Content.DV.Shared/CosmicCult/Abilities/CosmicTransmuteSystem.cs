using Content.Shared._DV.CosmicCult.Components;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._DV.CosmicCult.Abilities;

public sealed class CosmicTransmuteSystem : EntitySystem
{
    [Dependency] private readonly SharedCosmicCultSystem _cult = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedHandsSystem _hand = default!;
    [Dependency] private readonly PullingSystem _pull = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    private string? _message;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultComponent, EventCosmicTransmutation>(OnTransmute);
        SubscribeLocalEvent<CosmicTransmutableComponent, GetVerbsEvent<ExamineVerb>>(OnDetailedExamine);
    }

    private void OnDetailedExamine(Entity<CosmicTransmutableComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!_cult.EntityIsCultist(args.User) || !_proto.TryIndex(ent.Comp.TransmuteTo, out var result)) //non-cultists don't need to know this
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
    private void OnTransmute(Entity<CosmicCultComponent> ent, ref EventCosmicTransmutation args)
    {
        if (args.Handled)
            return;
        _message = null;

        if (!TryTransmutePulled(ent) && !TryTransmuteHeld(ent)) // That's some slightly less wonky code layout here
        {
            _popup.PopupClient(Loc.GetString(_message ?? "cosmicability-transmute-no-item"), ent, ent);
            return;
        }

        args.Handled = true;
        _audio.PlayPredicted(ent.Comp.TransmuteSFX, ent, ent);
        if (_net.IsServer) // Predicted spawn looks bad with animations
            PredictedSpawnAtPosition(ent.Comp.TransmuteVFX, Transform(ent).Coordinates);
    }

    private bool TryTransmutePulled(Entity<CosmicCultComponent> ent)
    {
        if (_pull.GetPulling(ent.Owner) is not { } target)
            return false;
        if (!TryComp<CosmicTransmutableComponent>(target, out var comp)
        || !_proto.TryIndex(comp.TransmuteTo, out var proto))
        {
            _message = "cosmicability-transmute-not-transmutable";
            return false;
        }
        if (comp.RequiresEmpowerment && !ent.Comp.CosmicEmpowered)
        {
            _message = "cosmicability-transmute-not-empowered";
            return false;
        }

        var tgtPos = Transform(target).Coordinates;
        var result = PredictedSpawnAtPosition(proto.ID, tgtPos);
        _pull.TryStartPull(ent, result);
        PredictedQueueDel(target);
        return true;
    }

    private bool TryTransmuteHeld(Entity<CosmicCultComponent> ent)
    {
        if (!_hand.TryGetActiveItem(ent.Owner, out var item)
        || item is not { } target)
            return false;
        if (!TryComp<CosmicTransmutableComponent>(target, out var comp)
        || !_proto.TryIndex(comp.TransmuteTo, out var proto))
        {
            _message ??= "cosmicability-transmute-not-transmutable";
            return false;
        }

        if (comp.RequiresEmpowerment && !ent.Comp.CosmicEmpowered)
        {
            _message ??= "cosmicability-transmute-not-empowered";
            return false;
        }

        var result = PredictedSpawnAtPosition(proto.ID, Transform(ent).Coordinates);
        _hand.TryDrop(ent.Owner);
        _hand.TryForcePickupAnyHand(ent.Owner, result);
        PredictedQueueDel(target);
        return true;
    }
}
