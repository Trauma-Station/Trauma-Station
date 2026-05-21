// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Interaction;
using Content.Trauma.Shared.Silicon.Components;

namespace Content.Trauma.Server.HTN.PrimitiveTasks.Operators.Specific;

[DataDefinition]
public sealed partial class PickPatrolCoordinateOperator : HTNOperator
{
    [Dependency] private IEntityManager _entMan = default!;
    private PathfindingSystem _pathfinding = default!;

    /// <summary>
    /// Target entitycoordinates to move to.
    /// </summary>
    [DataField(required: true)]
    public string TargetMoveKey = string.Empty;

    private EntityQuery<PatrolSlaveComponent> _slaveQuery = default!;
    private EntityQuery<PatrolCommanderComponent> _commanderQuery = default!;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();

        _slaveQuery = _entMan.GetEntityQuery<PatrolSlaveComponent>();
        _commanderQuery = _entMan.GetEntityQuery<PatrolCommanderComponent>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_slaveQuery.TryComp(owner, out var slave) ||
            !_commanderQuery.TryComp(slave.MasterEntity, out var master) ||
            !master.IsPatrolling)
            return (false, null);

        EntityUid? targetEntity = null;
        var waypoints = master.Waypoints;

        if (waypoints.Count <= 0)
            targetEntity = slave.MasterEntity;
        else
        {
            var nextTargetIndex = 0;

            if (blackboard.TryGetValue<EntityUid>("LastPatrolWaypoint", out var lastWaypoint, _entMan))
            {
                var currentIndex = 0;
                foreach (var waypoint in waypoints)
                {
                    if (waypoint == lastWaypoint)
                        break;
                    currentIndex++;
                }
                nextTargetIndex = (currentIndex + 1) % waypoints.Count;
            }

            var i = 0;
            foreach (var waypoint in waypoints)
            {
                if (i == nextTargetIndex)
                {
                    targetEntity = waypoint;
                    break;
                }
                i++;
            }
        }

        if (targetEntity is not { } target)
            return (false, null);

        var pathRange = SharedInteractionSystem.InteractionRange - 1f;
        var path = await _pathfinding.GetPath(owner, target, pathRange, cancelToken, PathFlags.Access);
        if (path.Result != PathResult.Path)
            return (false, null);

        return (true, new Dictionary<string, object>()
        {
            { "LastPatrolWaypoint", target },
            { TargetMoveKey, _entMan.GetComponent<TransformComponent>(target).Coordinates },
            { NPCBlackboard.PathfindKey, path },
        });
    }
}
