// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.Blob.GameTicking;
using Content.Goobstation.Server.Blob.Objectives;
using Content.Goobstation.Shared.Blob;
using Content.Goobstation.Shared.Blob.Components;
using Content.Server.AlertLevel;
using Content.Server.RoundEnd;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station;
using Content.Shared.Objectives.Components;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.CPUJob.JobQueues.Queues;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Goobstation.Server.Blob;

public sealed partial class BlobCoreSystem : SharedBlobCoreSystem
{
    [Dependency] private AlertLevelSystem _alertLevel = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private RoundEndSystem _roundEnd = default!;
    [Dependency] private SharedStationSystem _station = default!;

    private const double KillCoreJobTime = 0.5;
    private readonly JobQueue _killCoreJobQueue = new(KillCoreJobTime);

    public sealed class KillBlobCore(
        BlobCoreSystem system,
        EntityUid? station,
        Entity<BlobCoreComponent> ent,
        double maxTime,
        CancellationToken cancellation = default)
        : Job<object>(maxTime, cancellation)
    {
        protected override async Task<object?> Process()
        {
            system.DestroyBlobCore(ent, station);
            return null;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _killCoreJobQueue.Process();
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<BlobCoreComponent> ent, ref ComponentShutdown args)
    {
        CreateKillBlobCoreJob(ent);
    }

    #region Objective

    [SubscribeLocalEvent]
    private void OnBlobCaptureInfoAdd(Entity<BlobCaptureConditionComponent> ent, ref ObjectiveAssignedEvent args)
    {
        if (!TryComp<BlobObserverComponent>(args.Mind.OwnedEntity, out var observer))
        {
            args.Cancelled = true;
            return;
        }

        if (_station.GetOwningStation(observer.Core) is not { } station)
        {
            args.Cancelled = true;
            return;
        }

        ent.Comp.Target = CompOrNull<StationBlobConfigComponent>(station)?.StageTheEnd ?? StationBlobConfigComponent.DefaultStageEnd;
    }

    [SubscribeLocalEvent]
    private void OnBlobCaptureInfo(Entity<BlobCaptureConditionComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        _meta.SetEntityDescription(ent, Loc.GetString("objective-condition-blob-capture-description", ("count", ent.Comp.Target)));
    }

    [SubscribeLocalEvent]
    private void OnBlobCaptureProgress(Entity<BlobCaptureConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (!TryComp<BlobObserverComponent>(args.Mind.OwnedEntity, out var observer) ||
            !TryComp<BlobCoreComponent>(observer.Core, out var core))
        {
            args.Progress = 0;
            return;
        }

        var target = ent.Comp.Target;
        args.Progress = 0;

        args.Progress = target != 0
            ? MathF.Min((float) core.BlobTiles.Count / target, 1f)
            : 1f;
    }
    #endregion

    private void CreateKillBlobCoreJob(Entity<BlobCoreComponent> core)
    {
        PredictedQueueDel(core.Comp.Observer);

        var station = _station.GetOwningStation(core);
        var job = new KillBlobCore(this, station, core, KillCoreJobTime);
        _killCoreJobQueue.EnqueueJob(job);
    }

    private void DestroyBlobCore(Entity<BlobCoreComponent> core, EntityUid? stationUid)
    {
        foreach (var tile in core.Comp.BlobTiles.AsParallel())
        {
            if (!TileQuery.TryComp(tile, out var tileComp))
                continue;

            tileComp.Core = null;
            tileComp.Color = Color.White;
            Dirty(tile, tileComp);
        }

        var aliveBlobs = 0;
        foreach (var blob in EntityQueryEnumerator<BlobCoreComponent>())
        {
            if (!TerminatingOrDeleted(blob))
                aliveBlobs++;
        }

        if (aliveBlobs > 0)
            return;

        var blobRuleQuery = EntityQueryEnumerator<BlobRuleComponent, ActiveGameRuleComponent>();
        while (blobRuleQuery.MoveNext(out _, out var blobRuleComp, out _))
        {
            if (blobRuleComp.Stage is BlobStage.TheEnd or BlobStage.Default)
                continue;

            if (stationUid != null)
                _alertLevel.SetLevel(stationUid.Value, "green", true, true, true);

            _roundEnd.CancelRoundEndCountdown(forceRecall: false);
            blobRuleComp.Stage = BlobStage.Default;
        }
    }
}
