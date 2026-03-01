using Content.Goobstation.Common.Temperature.Components;
using Content.Server._DV.CosmicCult.EntitySystems;
using Content.Server.Actions;
using Content.Server.Antag;
using Content.Server.Atmos.Components;
using Content.Server.Audio;
using Content.Server.GameTicking.Events;
using Content.Server.Pinpointer;
using Content.Server.Popups;
using Content.Shared._DV.CosmicCult;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared.Eye;
using Content.Shared.Hands;
using Content.Shared.Humanoid;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Polymorph;
using Content.Shared.Radio.Components;
using Content.Shared.Speech.Components;
using Content.Shared.Speech;
using Content.Shared.Popups;
using Content.Shared.Temperature.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.EntitySerialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Trauma.Common.Speech;

namespace Content.Server._DV.CosmicCult;

public sealed partial class CosmicCultSystem : SharedCosmicCultSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly CosmicCultRuleSystem _cultRule = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    private readonly ResPath _mapPath = new("Maps/_DV/Nonstations/cosmicvoid.yml");
    private readonly SoundSpecifier _levelupReadySound = new SoundPathSpecifier("/Audio/_DV/CosmicCult/ascendant_noise.ogg");
    private readonly SoundSpecifier _levelupSound = new SoundPathSpecifier("/Audio/_DV/CosmicCult/tier_up.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);

        SubscribeLocalEvent<CosmicCultComponent, ComponentInit>(OnStartCultist);
        SubscribeLocalEvent<CosmicCultComponent, GetVisMaskEvent>(OnGetVisMask);

        SubscribeLocalEvent<CosmicEquipmentComponent, GotEquippedEvent>(OnGotCosmicItemEquipped);
        SubscribeLocalEvent<CosmicEquipmentComponent, GotUnequippedEvent>(OnGotCosmicItemUnequipped);
        SubscribeLocalEvent<CosmicEquipmentComponent, GotEquippedHandEvent>(OnGotHeld);
        SubscribeLocalEvent<CosmicEquipmentComponent, GotUnequippedHandEvent>(OnGotUnheld);

        SubscribeLocalEvent<InfluenceStrideComponent, ComponentInit>(OnStartInfluenceStride);
        SubscribeLocalEvent<InfluenceStrideComponent, ComponentRemove>(OnEndInfluenceStride);
        SubscribeLocalEvent<InfluenceStrideComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);
        SubscribeLocalEvent<CosmicImposingComponent, ComponentInit>(OnStartImposition);
        SubscribeLocalEvent<CosmicImposingComponent, ComponentRemove>(OnEndImposition);
        SubscribeLocalEvent<CosmicImposingComponent, RefreshMovementSpeedModifiersEvent>(OnImpositionMoveSpeed);

        SubscribeLocalEvent<CosmicCultComponent, PolymorphedEvent>(OnCultistPolymorphed);
        SubscribeLocalEvent<SpeechOverrideComponent, GotEquippedEvent>(OnGotSpeechOverrideEquipped);
        SubscribeLocalEvent<SpeechOverrideComponent, GotUnequippedEvent>(OnGotSpeechOverrideUnequipped);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var markQuery = EntityQueryEnumerator<CosmicSubtleMarkComponent>();
        while (markQuery.MoveNext(out var uid, out var comp))
            if (comp.ExpireTimer is { } timer && _timing.CurTime > timer)
                RemComp<CosmicSubtleMarkComponent>(uid);

        var echoQuery = EntityQueryEnumerator<CosmicMalignEchoComponent>();
        while (echoQuery.MoveNext(out var uid, out var comp))
            if (_timing.CurTime > comp.ExpireTimer)
                RemComp<CosmicMalignEchoComponent>(uid);

    }

    public override int AddEntropy(Entity<CosmicCultComponent> ent, int amount)
    {
        var realAmount = base.AddEntropy(ent, amount);

        _cultRule.IncrementCultObjectiveEntropy(ent, realAmount);
        Dirty(ent, ent.Comp);
        return realAmount;
    }

    public override void LevelUp(Entity<CosmicCultComponent> ent)
    {
        base.LevelUp(ent);
        _antag.SendBriefing(ent, Loc.GetString("cosmiccult-role-levelup-awaiting-input"), Color.FromHex("#4cabb3"), _levelupReadySound);
    }

    public override void OnLevelUpConfirmed(Entity<CosmicShopComponent> ent, ref LevelUpconfirmedMessage args)
    {
        base.OnLevelUpConfirmed(ent, ref args);

        if (!TryComp<CosmicCultComponent>(args.Actor, out var cultComp)) return;
        _cultRule.UpdateCultData((args.Actor, cultComp));
        _antag.SendBriefing(ent, Loc.GetString("cosmiccult-role-levelup-briefing"), Color.FromHex("#4cabb3"), _levelupSound);
    }

    #region Housekeeping

    // Rogue Ascendants use this too, which are generalized MidRoundAntags, so we keep the map around. If you're porting cosmic cult, and do not want rogue ascendants, feel free to move this into selective usage akin to NukeOps base.
    /// <summary>
    /// Creates the Cosmic Void pocket dimension map.
    /// </summary>
    private void OnRoundStart(RoundStartingEvent ev)
    {
        if (_mapLoader.TryLoadMap(_mapPath, out var map, out _, new DeserializationOptions { InitializeMaps = true }))
            _map.SetPaused(map.Value.Comp.MapId, false);
    }

    #endregion

    #region Init Cult
    /// <summary>
    /// Add the starting powers to the cultist.
    /// </summary>
    private void OnStartCultist(Entity<CosmicCultComponent> uid, ref ComponentInit args)
    {
        _eye.RefreshVisibilityMask(uid.Owner);
        if (!HasComp<HumanoidProfileComponent>(uid)) return; // Non-humanoids don't get abilities
        foreach (var actionId in uid.Comp.CosmicCultActions)
            _actions.AddAction(uid, actionId);

        uid.Comp.CosmicShopActionEntity = _actions.AddAction(uid, uid.Comp.CosmicShopAction);

        if (TryComp(uid, out EyeComponent? eyeComp))
            _eye.SetVisibilityMask(uid, eyeComp.VisibilityMask | (int) VisibilityFlags.CosmicCultMonument);
    }

    private void OnGetVisMask(Entity<CosmicCultComponent> uid, ref GetVisMaskEvent args)
    {
        args.VisibilityMask |= (int) VisibilityFlags.CosmicCultMonument;
    }
    #endregion

    #region Equipment Pickup
    private void OnGotCosmicItemEquipped(Entity<CosmicEquipmentComponent> ent, ref GotEquippedEvent args)
    {
        if (EntityIsCultist(args.Equipee, includeLesser: false)) return; // Lesser cultists can't use coscult equipment

        EnsureComp<CosmicDegenComponent>(args.Equipee);
    }

    private void OnGotCosmicItemUnequipped(Entity<CosmicEquipmentComponent> ent, ref GotUnequippedEvent args)
    {
        RemComp<CosmicDegenComponent>(args.Equipee); // Cultists shouldn't have it in the first place so we don't check if entity is a cultist
    }

    private void OnGotHeld(Entity<CosmicEquipmentComponent> ent, ref GotEquippedHandEvent args)
    {
        if (EntityIsCultist(args.User, includeLesser: false)) return;

        EnsureComp<CosmicDegenComponent>(args.User);
        _popup.PopupEntity(Loc.GetString("cosmiccult-gear-pickup", ("ITEM", args.Equipped)), args.User, args.User, PopupType.MediumCaution);
    }

    private void OnGotUnheld(Entity<CosmicEquipmentComponent> ent, ref GotUnequippedHandEvent args)
    {
        RemComp<CosmicDegenComponent>(args.User);
    }

    private void OnGotSpeechOverrideEquipped(Entity<SpeechOverrideComponent> ent, ref GotEquippedEvent args)
    {
        if (ent.Comp.EmoteIDs is { } emoteIDs && TryComp<VocalComponent>(args.Equipee, out var vocalComp))
        {
            ent.Comp.EmoteStoredIDs = vocalComp.Sounds;
            vocalComp.Sounds = emoteIDs;
            var ev = new EmoteSoundsChangedEvent();
            RaiseLocalEvent(args.Equipee, ref ev);
        }
        if (ent.Comp.SpeechIDs is { } speechIDs && TryComp<SpeechComponent>(args.Equipee, out var speechComp))
        {
            ent.Comp.SpeechStoredIDs = speechComp.SpeechSounds;
            speechComp.SpeechSounds = speechIDs;
            var ev = new SpeechSoundsChangedEvent();
            RaiseLocalEvent(args.Equipee, ref ev);
        }
    }

    private void OnGotSpeechOverrideUnequipped(Entity<SpeechOverrideComponent> ent, ref GotUnequippedEvent args)
    {
        if (ent.Comp.EmoteStoredIDs is { } emoteIDs && TryComp<VocalComponent>(args.Equipee, out var vocalComp))
        {
            ent.Comp.EmoteStoredIDs = null;
            vocalComp.Sounds = emoteIDs;
            var ev = new EmoteSoundsChangedEvent();
            RaiseLocalEvent(args.Equipee, ref ev);
        }
        if (ent.Comp.SpeechStoredIDs is { } speechIDs && TryComp<SpeechComponent>(args.Equipee, out var speechComp))
        {
            ent.Comp.SpeechStoredIDs = null;
            speechComp.SpeechSounds = speechIDs;
            var ev = new SpeechSoundsChangedEvent();
            RaiseLocalEvent(args.Equipee, ref ev);
        }
    }
    #endregion

    #region Movespeed
    private void OnStartImposition(Entity<CosmicImposingComponent> uid, ref ComponentInit args) => // these functions just make sure
        _movementSpeed.RefreshMovementSpeedModifiers(uid);
    private void OnEndImposition(Entity<CosmicImposingComponent> uid, ref ComponentRemove args) => // as various cosmic cult effects get added and removed
        _movementSpeed.RefreshMovementSpeedModifiers(uid);
    private void OnStartInfluenceStride(Entity<InfluenceStrideComponent> uid, ref ComponentInit args) => // that movespeed applies more-or-less correctly
        _movementSpeed.RefreshMovementSpeedModifiers(uid);
    private void OnEndInfluenceStride(Entity<InfluenceStrideComponent> uid, ref ComponentRemove args) => // i wish movespeed was easier to work with
        _movementSpeed.RefreshMovementSpeedModifiers(uid);

    private void OnRefreshMoveSpeed(EntityUid uid, InfluenceStrideComponent comp, RefreshMovementSpeedModifiersEvent args) =>
        args.ModifySpeed(1.4f, 1.4f);
    private void OnImpositionMoveSpeed(EntityUid uid, CosmicImposingComponent comp, RefreshMovementSpeedModifiersEvent args) =>
        args.ModifySpeed(0.80f, 0.80f);
    #endregion

    #region Edge cases
    /// <summary>
    /// When a cultist gets polymorphed, ensure that the resulting entity has all the necessary components. Thank god kitsune aren't real.
    /// Should probably just move most of this shit to mind instead at some point
    /// </summary>
    private void OnCultistPolymorphed(Entity<CosmicCultComponent> ent, ref PolymorphedEvent args)
    {
        if (_cultRule.AssociatedGamerule(args.OldEntity) is not { } cult)
            return;
        if (TryComp<CosmicCultComponent>(args.OldEntity, out var oldCultComp))
        {
            EnsureComp<CosmicCultComponent>(args.NewEntity, out var cultComp);
            cultComp.CosmicEmpowered = oldCultComp.CosmicEmpowered;
        }
        if (HasComp<CosmicStarMarkComponent>(args.OldEntity))
            EnsureComp<CosmicStarMarkComponent>(args.NewEntity);
        if (HasComp<CosmicSubtleMarkComponent>(args.OldEntity))
            EnsureComp<CosmicSubtleMarkComponent>(args.NewEntity);
        if (HasComp<SpecialLowTempImmunityComponent>(args.OldEntity))
            EnsureComp<SpecialLowTempImmunityComponent>(args.NewEntity);
        if (HasComp<PressureImmunityComponent>(args.OldEntity))
            EnsureComp<PressureImmunityComponent>(args.NewEntity);
        if (HasComp<CosmicNonRespiratingComponent>(args.OldEntity))
            EnsureComp<CosmicNonRespiratingComponent>(args.NewEntity);
        EnsureComp<IntrinsicRadioReceiverComponent>(args.NewEntity); // All cultists should have those, so we don't check for them separately
        EnsureComp<IntrinsicRadioTransmitterComponent>(args.NewEntity, out var transmitter);
        EnsureComp<ActiveRadioComponent>(args.NewEntity, out var radio);
        EnsureComp<CosmicCultAssociatedRuleComponent>(args.NewEntity, out var associatedComp);
        radio.Channels.Add("CosmicRadio");
        transmitter.Channels.Add("CosmicRadio");
        associatedComp.CultGamerule = cult;
    }
    #endregion
}
