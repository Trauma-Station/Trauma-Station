// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading;
using System.Threading.Tasks;
using Content.Goobstation.Shared.Silicon.Bots;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Emag.Components;
using Content.Shared.Interaction;

namespace Content.Goobstation.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed partial class PickNearbyServicableHydroponicsTrayOperator : HTNOperator
{
    [Dependency] private IEntityManager _ent = default!;
    private EntityLookupSystem _lookup = default!;
    private PathfindingSystem _pathfinding = default!;
    private PlantHolderSystem _holder = default!;
    private EntityQuery<EmaggedComponent> _emaggedQuery = default!;
    private EntityQuery<PlantHolderComponent> _holderQuery = default!;

    /// <summary>
    /// Determines how close the bot needs to be to service a tray
    /// </summary>
    public const float Range = 4f;

    /// <summary>
    /// Target entity to service
    /// </summary>
    public const string TargetKey = "PlantTarget";

    /// <summary>
    /// Target entitycoordinates to move to.
    /// </summary>
    [DataField(required: true)]
    public string TargetMoveKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);

        _lookup = sysManager.GetEntitySystem<EntityLookupSystem>();
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();

        _emaggedQuery = _ent.GetEntityQuery<EmaggedComponent>();
        _holderQuery = _ent.GetEntityQuery<PlantHolderComponent>();
    }

    private bool ShouldServiceTray(Entity<PlantTrayComponent> tray)
    {
        // fixing small problems in the tray
        if (tray.Comp.WaterLevel <= PlantbotServiceOperator.RequiredWaterLevelToService ||
            tray.Comp.WeedLevel >= PlantbotServiceOperator.RequiredWeedsAmountToWeed)
            return true;

        // harvesting the plant
        return tray.Comp.PlantEntity is { } plant &&
            _holderQuery.TryComp(plant, out var holder) &&
            holder.ReadyForHarvest;
    }

    private bool ShouldKillTray(Entity<PlantTrayComponent> tray)
    {
        if (tray.Comp.PlantEntity is not { } plant || _holder.IsDead(plant))
            return false;

        return tray.Comp.WaterLevel > 0f;
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        var emagged = _emaggedQuery.HasComp(owner);

        var coords = _ent.GetComponent<TransformComponent>(owner).Coordinates;
        var targets = _lookup.GetEntitiesInRange<PlantTrayComponent>(coords, Range, LookupFlags.Dynamic | LookupFlags.Static);
        foreach (var target in targets)
        {
            if (!(emagged ? ShouldKillTray(target) : ShouldServiceTray(target)))
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
