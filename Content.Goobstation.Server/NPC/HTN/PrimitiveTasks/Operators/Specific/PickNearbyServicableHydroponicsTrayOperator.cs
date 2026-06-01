// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading;
using System.Threading.Tasks;
using Content.Goobstation.Shared.Silicon.Bots;
using Content.Server.Botany.Components;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Emag.Components;
using Content.Shared.Interaction;

namespace Content.Goobstation.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed partial class PickNearbyServicableHydroponicsTrayOperator : HTNOperator
{
    [Dependency] private IEntityManager _ent = default!;
    private EntityLookupSystem _lookup = default!;
    private PathfindingSystem _pathfinding = default!;
    private EntityQuery<EmaggedComponent> _emaggedQuery = default!;

    /// <summary>
    /// Determines how close the bot needs to be to service a tray
    /// </summary>
    public const float Range = 4f;

    /// <summary>
    /// Target entity to service
    /// </summary>
    [DataField(required: true)]
    public string TargetKey = string.Empty;

    /// <summary>
    /// Target entitycoordinates to move to.
    /// </summary>
    [DataField(required: true)]
    public string TargetMoveKey = string.Empty;

    private HashSet<Entity<PlantHolderComponent>> _targets = new();

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);

        _lookup = sysManager.GetEntitySystem<EntityLookupSystem>();
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();

        _emaggedQuery = _ent.GetEntityQuery<EmaggedComponent>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        var emagged = _emaggedQuery.HasComp(owner);

        var coords = _ent.GetComponent<TransformComponent>(owner).Coordinates;
        _targets.Clear();
        _lookup.GetEntitiesInRange(coords, Range, _targets);
        foreach (var target in _targets)
        {
            if (target.Comp is { WaterLevel: >= PlantbotServiceOperator.RequiredWaterLevelToService, WeedLevel: <= PlantbotServiceOperator.RequiredWeedsAmountToWeed, Harvest: false } && (!emagged || target.Comp.Dead || target.Comp.WaterLevel <= 0f))
                continue;

            //Needed to make sure it doesn't sometimes stop right outside it's interaction range
            var pathRange = SharedInteractionSystem.InteractionRange - 1f;
            var path = await _pathfinding.GetPath(owner, target.Owner, pathRange, cancelToken);

            if (path.Result == PathResult.NoPath)
                continue;

            return (true, new Dictionary<string, object>()
            {
                {TargetKey, target.Owner},
                {TargetMoveKey, _ent.GetComponent<TransformComponent>(target).Coordinates},
                {NPCBlackboard.PathfindKey, path},
            });
        }

        return (false, null);
    }
}
