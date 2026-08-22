// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.Blob.GameTicking;
using Content.Goobstation.Shared.Blob;
using Content.Goobstation.Shared.Blob.Components;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.Actions;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using System.Linq;

namespace Content.Goobstation.Server.Blob;

public sealed partial class BlobObserverSystem : SharedBlobObserverSystem
{
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    private static readonly EntProtoId BlobCaptureObjective = "BlobCaptureObjective";
    private static readonly EntProtoId BlobRule = "BlobRule";

    private const double MoverJobTime = 0.005;
    private readonly JobQueue _moveJobQueue = new(MoverJobTime);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobCoreComponent, PlayerAttachedEvent>(OnCorePlayerAttached, before: [typeof(SharedActionsSystem)]);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _moveJobQueue.Process();
    }

    private void OnCorePlayerAttached(Entity<BlobCoreComponent> ent, ref PlayerAttachedEvent args)
    {
        var xform = Transform(ent);
        if (!_gridQuery.HasComp(xform.GridUid))
            return;

        if (!TerminatingOrDeleted(ent.Comp.Observer))
            return;

        CreateBlobObserver(ent, args.Player.UserId);
    }

    // TODO: This is very bad, but it is clearly better than invisible walls, let someone do better.
    [SubscribeLocalEvent]
    private void OnMoveEvent(Entity<BlobObserverComponent> ent, ref MoveEvent args)
    {
        if (ent.Comp.IsProcessingMoveEvent)
            return;

        ent.Comp.IsProcessingMoveEvent = true;

        var job = new BlobObserverMover(EntityManager, Xform, this, MoverJobTime)
        {
            Observer = ent,
            NewPosition = args.NewPosition
        };

        _moveJobQueue.EnqueueJob(job);
    }

    public void CreateBlobObserver(Entity<BlobCoreComponent> core, NetUserId userId)
    {
        var coords = Transform(core).Coordinates;
        var observer = PredictedSpawnAtPosition(core.Comp.ObserverBlobPrototype, coords);
        var observerComp = Comp<BlobObserverComponent>(observer);

        core.Comp.Observer = observer;
        DirtyField(core, core.Comp, nameof(BlobCoreComponent.Observer));

        observerComp.Core = core;
        Dirty(observer, observerComp);

        var isNewMind = false;
        if (!_mind.TryGetMind(core.Owner, out var mindId, out var mind))
        {
            if (!_player.TryGetSessionById(userId, out var session) ||
                session.AttachedEntity is not { } uid ||
                !_mind.TryGetMind(uid, out mindId, out mind))
            {
                (mindId, mind) = _mind.CreateMind(userId, "Blob Player");
                isNewMind = true;
            }
        }

        if (!isNewMind)
        {
            var name = "???";
            if (_player.TryGetSessionById(mind.UserId, out var session1))
                name = session1.Name;
            _mind.WipeMind(mindId, mind);
            (mindId, mind) = _mind.CreateMind(userId, $"Blob Player ({name})");
        }

        _role.MindAddRole(mindId, core.Comp.MindRoleBlobPrototypeId.Id);
        SendBlobBriefing(mindId);

        var ruleExists = false;
        foreach (var rule in EntityQueryEnumerator<BlobRuleComponent>())
        {
            // TODO: check station or something
            rule.Comp.Blobs.Add((mindId, mind));
            ruleExists = true;
        }

        if (!ruleExists)
        {
            _ticker.StartGameRule(BlobRule, out var rule);
            Comp<BlobRuleComponent>(rule).Blobs.Add((mindId, mind));
        }

        _mind.TransferTo(mindId, observer, true, mind: mind);

        _mind.TryAddObjective(mindId, mind, BlobCaptureObjective);
    }

    private void SendBlobBriefing(EntityUid mind)
    {
        if (_player.TryGetSessionByEntity(mind, out var session))
        {
            _chat.DispatchServerMessage(session, Loc.GetString("blob-role-greeting"));
        }
    }
}
