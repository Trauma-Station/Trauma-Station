// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Silicon.Components;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Intellicard;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using static Content.Shared.Movement.Systems.SharedContentEyeSystem;

namespace Content.Goobstation.Shared.Silicon;

public sealed partial class IntellicardExtrasSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private NameModifierSystem _nameMod = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedEyeSystem _eye = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly EntProtoId DefaultAi = "StationAiBrain";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IntellicardComponent, AfterInteractEvent>(OnHolderInteract);
        SubscribeLocalEvent<IntellicardableMindComponent, IntellicardDoAfterEvent>(OnIntellicardDoAfter);
    }

    private void OnHolderInteract(Entity<IntellicardComponent> ent, ref AfterInteractEvent args)
    {
        var user = args.User;
        if (args.Handled || !args.CanReach ||
            args.Target is not { } target ||
            !TryComp(target, out IntellicardableMindComponent? comp) ||
            !TryComp(args.Used, out StationAiHolderComponent? cardAiHolder))
            return;

        // this only exists after the borg has been inhabited at least once, stock positronic brains dont have it...
        // so we need to create it if its not there already (arguably a bug because other brains do have it on spawn...)
        var targetMind = EnsureComp<MindContainerComponent>(target);

        // because of the stupid ai holder thing, need to remove the brain if the ai mind is destroyed due to suicide or something
        // otherwise a braindead ai clogs the card without any clear indicator.
        var cardBrain = _slots.GetItemOrNull(ent.Owner, "station_ai_mind_slot");
        if (TryComp<MindContainerComponent>(cardBrain, out var cardMind) && !cardMind.HasMind)
        {
            _popup.PopupEntity(Loc.GetString("intellicard-extras-contained-missing"), user, user, PopupType.MediumCaution);
            PredictedQueueDel(cardBrain);
            args.Handled = true;
            return;
        }

        var cardHasAi = _slots.CanEject(ent.Owner, user, cardAiHolder.Slot);
        var brainHasAi = targetMind.HasMind;

        if (cardHasAi == brainHasAi)
        {
            var key = cardHasAi ? "occupied" : "empty";
            _popup.PopupEntity(Loc.GetString($"intellicard-extras-target-{key}"), user, user, PopupType.Medium);
            args.Handled = true;
            return;
        }

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            user,
            cardHasAi ? ent.Comp.UploadTime * comp.UploadTimeFactor : ent.Comp.DownloadTime * comp.DownloadTimeFactor,
            new IntellicardDoAfterEvent(),
            eventTarget: args.Target,
            used: ent.Owner,
            target: args.Target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            BreakOnDropItem = true,
            MultiplyDelay = false
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnIntellicardDoAfter(Entity<IntellicardableMindComponent> ent, ref IntellicardDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used is not { } cardUid)
            return;

        if (!TryComp(cardUid, out StationAiHolderComponent? targetHolder))
            return;

        var cardBrain = _slots.GetItemOrNull(cardUid, StationAiCoreComponent.Container);

        TryComp<MindContainerComponent>(cardBrain, out var cardMindContainer);

        if (!TryComp(cardUid, out StationAiHolderComponent? cardAiHolder))
            return;

        if (!TryComp(ent.Owner, out MindContainerComponent? targetMindContainer))
            return;

        var user = args.User;

        // get mind status of both
        var cardHasAi = _slots.CanEject(cardUid, user, cardAiHolder.Slot) && cardMindContainer?.HasMind == true;
        var targetHasAi = targetMindContainer.Mind is { };

        // Card -> Brain
        // upload the mind from the positronic brain inside the card's StationAiHolder into the target brain
        if (cardHasAi && !targetHasAi)
        {
            _meta.SetEntityName(ent.Owner, _nameMod.GetBaseName(cardBrain!.Value));

            if (cardMindContainer?.Mind is { } cardMind)
            {
                _mind.TransferTo(cardMind, ent.Owner, ghostCheckOverride: true);
                _mind.UnVisit(cardMind);
            }

            PredictedQueueDel(cardBrain); // free up the empty brain

            _audio.PlayPredicted(cardAiHolder.Slot.InsertSound, ent, user);

            args.Handled = true;
            return;
        }

        // Brain -> Card
        // create positronic brain for StationAiHolder and then download the mind from the target brain
        if (!cardHasAi && targetHasAi)
        {
            if (!targetMindContainer.HasMind)
                return;

            var newCardBrain = PredictedSpawnInContainerOrDrop(DefaultAi, cardUid, StationAiCoreComponent.Container);
            _meta.SetEntityName(newCardBrain, _nameMod.GetBaseName(ent.Owner));

            if (targetMindContainer.Mind is { } targetMind)
            {
                _mind.TransferTo(targetMind, newCardBrain, ghostCheckOverride: true);
                _mind.UnVisit(targetMind);
            }

            _audio.PlayPredicted(cardAiHolder.Slot.InsertSound, cardUid, user);

            // for some godforsaken reason the fov gets fucked
            _eye.SetDrawFov(newCardBrain, true);

            args.Handled = true;
            return;
        }
    }
}

[Serializable, NetSerializable]
public sealed partial class IntellicardDoAfterEvent : SimpleDoAfterEvent;
