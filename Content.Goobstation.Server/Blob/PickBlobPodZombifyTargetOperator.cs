// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;

namespace Content.Goobstation.Server.Blob;

public sealed partial class PickBlobPodZombifyTargetOperator : HTNOperator
{
    [Dependency] private IEntityManager _ent = default!;
    private NpcFactionSystem _factions = default!;
    private MobStateSystem _mob = default!;
    private EntityQuery<HumanoidProfileComponent> _humanoidQuery = default!;
    private EntityQuery<TransformComponent> _xformQuery = default!;

    private EntityLookupSystem _lookup = default!;
    private PathfindingSystem _pathfinding = default!;

    public const float Range = 7f;

    [DataField(required: true)]
    public string TargetKey = string.Empty;

    [DataField(required: true)]
    public string ZombifyKey = string.Empty;

    /// <summary>
    /// Where the pathfinding result will be stored (if applicable). This gets removed after execution.
    /// </summary>
    [DataField]
    public string PathfindKey = NPCBlackboard.PathfindKey;

    private List<EntityUid> _targets = new();

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _lookup = sysManager.GetEntitySystem<EntityLookupSystem>();
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
        _mob = sysManager.GetEntitySystem<MobStateSystem>();
        _factions = sysManager.GetEntitySystem<NpcFactionSystem>();

        _humanoidQuery = _ent.GetEntityQuery<HumanoidProfileComponent>();
        _xformQuery = _ent.GetEntityQuery<TransformComponent>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        _targets.Clear();
        foreach (var entity in _factions.GetNearbyHostiles(owner, Range))
        {
            if (!_humanoidQuery.HasComp(entity))
                continue;

            if (_mob.IsAlive(entity))
                continue;

            _targets.Add(entity);
        }

        foreach (var target in _targets)
        {
            if (!_xformQuery.TryGetComponent(target, out var xform))
                continue;

            var targetCoords = xform.Coordinates;
            var path = await _pathfinding.GetPath(owner, target, Range, cancelToken);
            if (path.Result != PathResult.Path)
                continue;

            return (true, new Dictionary<string, object>()
            {
                { TargetKey, targetCoords },
                { ZombifyKey, target },
                { PathfindKey, path }
            });
        }

        return (false, null);
    }
}
