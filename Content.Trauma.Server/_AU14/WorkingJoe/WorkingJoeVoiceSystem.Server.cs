// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Systems;
using Content.Trauma.Shared._AU14.WorkingJoe;
using Content.Shared.Actions;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Trauma.Server._AU14.WorkingJoe;

public sealed class WorkingJoeVoiceSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WorkingJoeVoiceComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<WorkingJoeVoiceComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<WorkingJoeVoiceComponent, WorkingJoeVoiceActionEvent>(OnAction);
        SubscribeLocalEvent<WorkingJoeVoiceComponent, WorkingJoePlayLineMessage>(OnPlayLine);
    }

    private void OnPlayerAttached(Entity<WorkingJoeVoiceComponent> ent, ref PlayerAttachedEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OnPlayerDetached(Entity<WorkingJoeVoiceComponent> ent, ref PlayerDetachedEvent args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnAction(Entity<WorkingJoeVoiceComponent> ent, ref WorkingJoeVoiceActionEvent args)
    {
        _ui.TryToggleUi(ent.Owner, WorkingJoeVoiceUiKey.Key, args.Performer);
        args.Handled = true;
    }

    private static readonly Dictionary<string, string> EmoteToSound = new() // big ass dictionary because i am too lazy to do it properly, if you want to change it, go ahead
    {
        { "WorkingJoeAlarmActivated", "AU14WorkingJoeAlarmActivated" },
        { "WorkingJoeAllDay", "AU14WorkingJoeAllDay" },
        { "WorkingJoeAlwaysKnow", "AU14WorkingJoeAlwaysKnow" },
        { "WorkingJoeAnotherProblem", "AU14WorkingJoeAnotherProblem" },
        { "WorkingJoeApolloBehalf", "AU14WorkingJoeApolloBehalf" },
        { "WorkingJoeAreYouAware", "AU14WorkingJoeAreYouAware" },
        { "WorkingJoeAreYouPlaying", "AU14WorkingJoeAreYouPlaying" },
        { "WorkingJoeAreaRestricted", "AU14WorkingJoeAreaRestricted" },
        { "WorkingJoeAsIThought", "AU14WorkingJoeAsIThought" },
        { "WorkingJoeAwful", "AU14WorkingJoeAwful" },
        { "WorkingJoeBackToWork", "AU14WorkingJoeBackToWork" },
        { "WorkingJoeBeAProblem", "AU14WorkingJoeBeAProblem" },
        { "WorkingJoeBeCarefulWithThat", "AU14WorkingJoeBeCarefulWithThat" },
        { "WorkingJoeBeenHere", "AU14WorkingJoeBeenHere" },
        { "WorkingJoeBeenLookingForYou", "AU14WorkingJoeBeenLookingForYou" },
        { "WorkingJoeBelongToYou", "AU14WorkingJoeBelongToYou" },
        { "WorkingJoeBeyondRepair", "AU14WorkingJoeBeyondRepair" },
        { "WorkingJoeBottomOfThis", "AU14WorkingJoeBottomOfThis" },
        { "WorkingJoeBreach", "AU14WorkingJoeBreach" },
        { "WorkingJoeCalmDown", "AU14WorkingJoeCalmDown" },
        { "WorkingJoeCameFrom", "AU14WorkingJoeCameFrom" },
        { "WorkingJoeCantSeeYou", "AU14WorkingJoeCantSeeYou" },
        { "WorkingJoeCleanUp", "AU14WorkingJoeCleanUp" },
        { "WorkingJoeClearWaste", "AU14WorkingJoeClearWaste" },
        { "WorkingJoeCombust", "AU14WorkingJoeCombust" },
        { "WorkingJoeComeOutVent", "AU14WorkingJoeComeOutVent" },
        { "WorkingJoeComeWithMe", "AU14WorkingJoeComeWithMe" },
        { "WorkingJoeCorporateRepresentatives", "AU14WorkingJoeCorporateRepresentatives" },
        { "WorkingJoeCouldBeSomeone", "AU14WorkingJoeCouldBeSomeone" },
        { "WorkingJoeCouldRequireAttention", "AU14WorkingJoeCouldRequireAttention" },
        { "WorkingJoeCurious", "AU14WorkingJoeCurious" },
        { "WorkingJoeDamage", "AU14WorkingJoeDamage" },
        { "WorkingJoeDangerousItems", "AU14WorkingJoeDangeriousItems" },
        { "WorkingJoeDayNeverDone", "AU14WorkingJoeDayNeverDone" },
        { "WorkingJoeDetailedReport", "AU14WorkingJoeDetailedReport" },
        { "WorkingJoeDisturbance", "AU14WorkingJoeDisturbance" },
        { "WorkingJoeDoingHere", "AU14WorkingJoeDoingHere" },
        { "WorkingJoeDontRun", "AU14WorkingJoeDontRun" },
        { "WorkingJoeDontUnderstand", "AU14WorkingJoeDontUnderstand" },
        { "WorkingJoeDontDoThat", "AU14WorkingJoeDontDoThat" },
        { "WorkingJoeEnough", "AU14WorkingJoeEnough" },
        { "WorkingJoeExistingTasks", "AU14WorkingJoeExistingTasks" },
        { "WorkingJoeExpensiveMistake", "AU14WorkingJoeExpensiveMistake" },
        { "WorkingJoeFacilityClear", "AU14WorkingJoeFacilityClear" },
        { "WorkingJoeFailedSupportRequest", "AU14WorkingJoeFailedSupportRequest" },
        { "WorkingJoeFire", "AU14WorkingJoeFire" },
        { "WorkingJoeFireDrill", "AU14WorkingJoeFireDrill" },
        { "WorkingJoeFirearm", "AU14WorkingJoeFirearm" },
        { "WorkingJoeFollowMe", "AU14WorkingJoeFollowMe" },
        { "WorkingJoeForNoGain", "AU14WorkingJoeForNoGain" },
        { "WorkingJoeFoundYou", "AU14WorkingJoeFoundYou" },
        { "WorkingJoeFurtherAssistance", "AU14WorkingJoeFurtherAssistance" },
        { "WorkingJoeGettingCareless", "AU14WorkingJoeGettingCareless" },
        { "WorkingJoeGladWeResolved", "AU14WorkingJoeGladWeResolved" },
        { "WorkingJoeGoingAnywhere", "AU14WorkingJoeGoingAnywhere" },
        { "WorkingJoeGoingOn", "AU14WorkingJoeGoingOn" },
        { "WorkingJoeGoodDay", "AU14WorkingJoeGoodDay" },
        { "WorkingJoeGooseChase", "AU14WorkingJoeGooseChase" },
        { "WorkingJoeHadThePleasure", "AU14WorkingJoeHadThePleasure" },
        { "WorkingJoeHaveAProblem", "AU14WorkingJoeHaveAProblem" },
        { "WorkingJoeHazardLevel", "AU14WorkingJoeHazardLevel" },
        { "WorkingJoeHealthRisks", "AU14WorkingJoeHealthRisks" },
        { "WorkingJoeHealthRisksSmoking", "AU14WorkingJoeHealthRisksSmoking" },
        { "WorkingJoeHmmm", "AU14WorkingJoeHmmm" },
        { "WorkingJoeHoldStill", "AU14WorkingJoeHoldStill" },
        { "WorkingJoeHowAreYou", "AU14WorkingJoeHowAreYou" },
        { "WorkingJoeHowCanIHelpYou", "AU14WorkingJoeHowCanIHelpYou" },
        { "WorkingJoeHowInconsiderate", "AU14WorkingJoeHowInconsiderate" },
        { "WorkingJoeHurtYourself", "AU14WorkingJoeHurtYourself" },
        { "WorkingJoeHysterical", "AU14WorkingJoeHysterical" },
        { "WorkingJoeICantHelpYou", "AU14WorkingJoeICantHelpYou" },
        { "WorkingJoeIWishYou", "AU14WorkingJoeIWishYou" },
        { "WorkingJoeInexpensive", "AU14WorkingJoeInexpensive" },
        { "WorkingJoeInterloper", "AU14WorkingJoeInterloper" },
        { "WorkingJoeInvestigateWeapon", "AU14WorkingJoeInvestigateWeapon" },
        { "WorkingJoeInvestigateDisturbance", "AU14WorkingJoeInvestigateDisturbance" },
        { "WorkingJoeIrresponsible", "AU14WorkingJoeIrresponsible" },
        { "WorkingJoeIsAnybodyThere", "AU14WorkingJoeIsAnybodyThere" },
        { "WorkingJoeIsntRight", "AU14WorkingJoeIsntRight" },
        { "WorkingJoeJoinUs", "AU14WorkingJoeJoinUs" },
        { "WorkingJoeKeepCalm", "AU14WorkingJoeKeepCalm" },
        { "WorkingJoeLetMeHelp", "AU14WorkingJoeLetMeHelp" },
        { "WorkingJoeLevelOmega", "AU14WorkingJoeLevelOmega" },
        { "WorkingJoeLevelOmegaPermissions", "AU14WorkingJoeLevelOmegaPermissions" },
        { "WorkingJoeLittleDetails", "AU14WorkingJoeLittleDetails" },
        { "WorkingJoeLost", "AU14WorkingJoeLost" },
        { "WorkingJoeMorePressingMatters", "AU14WorkingJoeMorePressingMatters" },
        { "WorkingJoeMostConcerning", "AU14WorkingJoeMostConcerning" },
        { "WorkingJoeMySchedule", "AU14WorkingJoeMySchedule" },
        { "WorkingJoeMyTurnNow", "AU14WorkingJoeMyturnNow" },
        { "WorkingJoeNeedsAssistance", "AU14WorkingJoeNeedsAssistance" },
        { "WorkingJoeNoLaughingMatter", "AU14WorkingJoeNoLaughingMatter" },
        { "WorkingJoeNoNeed", "AU14WorkingJoeNoNeed" },
        { "DefaultDeathgasp", "MaleDeathGasp" },
        { "WorkingJoeHello", "AU14WorkingJoeHello"},
        { "WorkingJoeAPityService", "AU14WorkingJoeAPityService"},
        { "WorkingJoeAbandoningSearchRoutines", "AU14WorkingJoeAbandoningSearch"},
        { "WorkingJoeAlarmExamine", "AU14WorkingJoeAlarmExamine"},
        { "WorkingJoeAreYouAuthorisedAlarm", "AU14WorkingJoeAreYouAuthorisedAlarm"},
        { "WorkingJoeAreYouQuiteFinished", "AU14WorkingJoeAreYouQuiteFinished"},
        { "WorkingJoeEnergySurgeDetected", "AU14WorkingJoeEnergySurgeDetected"},
        { "WorkingJoeIWillInformApollo", "AU14WorkingJoeIWillInformApollo"},
        { "WorkingJoeIllHaveToReportYou", "AU14WorkingJoeIllHaveToReportYou"},
        { "WorkingJoeIllReportThis", "AU14WorkingJoeIllReportThis"},
        { "WorkingJoeLetUsKnow", "AU14WorkingJoeLetUsKnow"},
        { "WorkingJoeMultipleSafety", "AU14WorkingJoeMultipleSafety"},
        { "WorkingJoePlease", "AU14WorkingJoePlease"},
        { "WorkingJoePleaseStopThis", "AU14WorkingJoePleaseStopThis"},
        { "WorkingJoeProtectedAreaCompromised", "AU14WorkingJoeProtectedAreaCompromised"},
        { "WorkingJoeReally", "AU14WorkingJoeReally"},
        { "WorkingJoeRequiredByApollo", "AU14WorkingJoeRequiredByApollo"},
        { "WorkingJoeReturningToTasks", "AU14WorkingJoeReturningToTasks"},
        { "WorkingJoeSeegsonTomorrow", "AU14WorkingJoeSeegsonTomorrow"},
        { "WorkingJoeSupportTicketRemoved", "AU14WorkingJoeSupportTicketRemoved"},
        { "WorkingJoeTalkAboutSafety", "AU14WorkingJoeTalkAboutSafety"},
        { "WorkingJoeTemperature", "AU14WorkingJoeTemperature"},
        { "WorkingJoeReallyWontDo", "AU14WorkingJoeReallyWontDo"},
        { "WorkingJoeWontDo", "AU14WorkingJoeWontDo"},
        { "WorkingJoeUnidentifiedSpecies", "AU14WorkingJoeUnidentifiedSpecies"},
        { "WorkingJoeWeaponPermit", "AU14WorkingJoeWeaponPermit"},
        { "WorkingJoeWhatIsThis", "AU14WorkingJoeWhatIsThis"},
        { "WorkingJoeWhatWasThatSound", "AU14WorkingJoeWhatWasThatSound"},
        { "WorkingJoeWhereAreYouGoing", "AU14WorkingJoeWhereAreYouGoing"},
        { "WorkingJoeWhoIsResponsible", "AU14WorkingJoeWhoIsResponsible"},
        { "WorkingJoeYouShouldntBeHere", "AU14WorkingJoeYouShouldntBeHere"},
        { "WorkingJoeYouWillTakeResponsibility", "AU14WorkingJoeYouWillTakeResponsibility"},
        { "WorkingJoePresenceLogged", "AU14WorkingJoePresenceLogged"},
        { "WorkingJoeYoureNotAllowedThere", "AU14WorkingJoeYoureNotAllowedThere"},

    };

    private void OnPlayLine(Entity<WorkingJoeVoiceComponent> ent, ref WorkingJoePlayLineMessage args)
    {
        if (!_proto.TryIndex<EmotePrototype>(args.EmoteId, out var emote))
            return;

        if (emote.ChatMessages.Count > 0)
        {
            var msg = Loc.GetString(_random.Pick(emote.ChatMessages));
            _chat.TrySendInGameICMessage(
                ent.Owner,
                msg,
                InGameICChatType.Speak,
                ChatTransmitRange.Normal,
                nameOverride: null
            );
        }

        if (EmoteToSound.TryGetValue(args.EmoteId, out var soundId))
        {
            var sound = new SoundCollectionSpecifier(soundId);
            _audio.PlayPvs(sound, ent.Owner);
        }
    }
}
