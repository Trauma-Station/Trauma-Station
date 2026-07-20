// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Goobstation.Common.Blob;
using Content.Goobstation.Shared.Blob.Components;
using Content.Server.AlertLevel;
using Content.Server.Antag;
using Content.Server.Audio;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Objectives;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Audio;
using Content.Shared.GameTicking.Components;
using Content.Shared.Objectives.Components;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Goobstation.Server.Blob.GameTicking;

public sealed partial class BlobRuleSystem : GameRuleSystem<BlobRuleComponent>
{
    [Dependency] private RoundEndSystem _roundEnd = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private AlertLevelSystem _alertLevel = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private IChatManager _chatMan = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private EmergencyShuttleSystem _emergency = default!;
    [Dependency] private ServerGlobalSoundSystem _sound = default!;

    protected override void Started(EntityUid uid, BlobRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        var activeRules = QueryActiveRules();
        while (activeRules.MoveNext(out var entityUid, out _, out _, out _))
        {
            if (uid == entityUid)
                continue;

            GameTicker.EndGameRule(uid, gameRule);
            Log.Warning("blob is active!!! remove!");
            break;
        }
    }

    protected override void ActiveTick(EntityUid uid, BlobRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        component.Accumulator += frameTime;

        if (component.Accumulator < 10)
            return;

        component.Accumulator = 0;

        var check = new Dictionary<EntityUid, long>();
        var blobCoreQuery = EntityQueryEnumerator<BlobCoreComponent, MetaDataComponent, TransformComponent>();
        while (blobCoreQuery.MoveNext(out var ent, out var comp, out var md, out var xform))
        {
            if (TerminatingOrDeleted(ent, md) ||
                !CheckBlobInStation(ent, xform, out var stationUid))
            {
                continue;
            }

            check.TryAdd(stationUid.Value, 0);

            check[stationUid.Value] += comp.BlobTiles.Count;
        }

        foreach (var (station, length) in check.AsParallel())
        {
            CheckChangeStage(station, component, length);
        }
    }

    private bool CheckBlobInStation(EntityUid blobCore, TransformComponent? xform, [NotNullWhen(true)] out EntityUid? stationUid)
    {
        var station = _station.GetOwningStation(blobCore, xform);
        if (station == null || !HasComp<StationEventEligibleComponent>(station.Value))
        {
            _chatMan.SendAdminAlert(blobCore, Loc.GetString("blob-alert-out-off-station"));
            QueueDel(blobCore);
            stationUid = null;
            return false;
        }

        stationUid = station.Value;
        return true;
    }

    private const string StationAlertCritical = "delta";
    private const string StationAlertDetected = "red";

    private void CheckChangeStage(
        Entity<StationBlobConfigComponent?> stationUid,
        BlobRuleComponent blobRuleComp,
        long blobTilesCount)
    {
        Resolve(stationUid, ref stationUid.Comp, false);

        var stationName = Name(stationUid);

        if (blobTilesCount >= (stationUid.Comp?.StageBegin ?? StationBlobConfigComponent.DefaultStageBegin)
            && _roundEnd.ExpectedCountdownEnd != null
            && !_emergency.EmergencyShuttleArrived)
        {
            _roundEnd.CancelRoundEndCountdown(forceRecall: true);
            _chat.DispatchStationAnnouncement(stationUid,
                Loc.GetString("blob-alert-recall-shuttle"),
                Loc.GetString("Station"),
                false,
                null,
                Color.Red);
        }
        else if (blobTilesCount >= (stationUid.Comp?.StageBegin ?? StationBlobConfigComponent.DefaultStageBegin)
                 && _roundEnd.ExpectedCountdownEnd != null && _emergency.EmergencyShuttleArrived)
        {
            _chat.DispatchStationAnnouncement(stationUid,
                Loc.GetString("blob-alert-shuttle-arrived"),
                Loc.GetString("Station"),
                false,
                null,
                Color.OrangeRed);
        }

        switch (blobRuleComp.Stage)
        {
            case BlobStage.Default when blobTilesCount >= (stationUid.Comp?.StageBegin ?? StationBlobConfigComponent.DefaultStageBegin):
                blobRuleComp.Stage = BlobStage.Begin;

                _chat.DispatchGlobalAnnouncement(
                    Loc.GetString("blob-alert-detect"),
                    stationName,
                    true,
                    null,
                    Color.Red);

                if (blobRuleComp.DetectedAudio is { } detectedAudio)
                    // Station is the source here because that's the only UID we have in this method. Гойда.
                    _sound.DispatchStationEventMusic(stationUid, detectedAudio, StationEventMusicType.Blob, detectedAudio.Params);

                _alertLevel.SetLevel(stationUid, StationAlertDetected, true, true, true, true);

                RaiseLocalEvent(stationUid,
                    new BlobChangeLevelEvent
                    {
                        Station = stationUid,
                        Level = blobRuleComp.Stage
                    },
                    broadcast: true);
                return;
            case BlobStage.Begin when blobTilesCount >= (stationUid.Comp?.StageCritical ?? StationBlobConfigComponent.DefaultStageCritical):
                blobRuleComp.Stage = BlobStage.Critical;
                _chat.DispatchGlobalAnnouncement(
                    Loc.GetString("blob-alert-critical-cburn"),
                    stationName,
                    true,
                    blobRuleComp.CriticalAudio,
                    Color.Red);

                if (blobRuleComp.CriticalAudio is { } criticalAudio)
                {
                    _sound.StopStationEventMusic(stationUid, StationEventMusicType.Blob);
                    _sound.DispatchStationEventMusic(stationUid, criticalAudio, StationEventMusicType.Blob, criticalAudio.Params);
                }

                if (!blobRuleComp.BlobCBurnCalled)
                    _ticker.StartGameRule(blobRuleComp.BlobCBurnEvent);
                blobRuleComp.BlobCBurnCalled = true;

                _alertLevel.SetLevel(stationUid, StationAlertCritical, true, true, true, true);

                RaiseLocalEvent(stationUid,
                    new BlobChangeLevelEvent
                    {
                        Station = stationUid,
                        Level = blobRuleComp.Stage
                    },
                    broadcast: true);
                return;

            case BlobStage.Critical when blobTilesCount >= (stationUid.Comp?.StageTheEnd ?? StationBlobConfigComponent.DefaultStageEnd):
                blobRuleComp.Stage = BlobStage.TheEnd;
                _roundEnd.EndRound();
                _sound.StopStationEventMusic(stationUid, StationEventMusicType.Blob);

                RaiseLocalEvent(stationUid,
                    new BlobChangeLevelEvent
                    {
                        Station = stationUid,
                        Level = blobRuleComp.Stage
                    },
                    broadcast: true);
                return;
        }
    }

    public void MakeBlob(EntityUid player)
    {
        var comp = EnsureComp<BlobCarrierComponent>(player);
        comp.HasMind = HasComp<ActorComponent>(player);
        comp.TransformationDelay = 10 * 60; // 10min
    }

    [SubscribeLocalEvent]
    private void AfterAntagSelected(EntityUid uid, BlobRuleComponent component, AfterAntagEntitySelectedEvent args)
    {
        MakeBlob(args.EntityUid);
    }
}
