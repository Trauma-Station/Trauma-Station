// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading;
using System.Threading.Tasks;
using Content.Goobstation.Shared.Xenobiology;
using Content.Goobstation.Shared.Xenobiology.Components;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Collections;

namespace Content.Goobstation.Server.Xenobiology.HTN;

public sealed partial class PickSlimeLatchTargetOperator : HTNOperator
{
    [Dependency] private IEntityManager _ent = default!;
    private NpcFactionSystem _factions = default!;
    private MobStateSystem _mobSystem = default!;
    private PathfindingSystem _pathfinding = default!;
    private SatiationSystem _satiation = default!;
    private SlimeLatchSystem _latch = default!;
    private EntityQuery<BeingLatchedComponent> _latchedQuery = default!;
    private EntityQuery<SatiationComponent> _satiationQuery = default!;
    private EntityQuery<SlimeDamageOvertimeComponent> _dotQuery = default!;

    [DataField(required: true)]
    public string RangeKey = string.Empty;

    [DataField(required: true)]
    public string TargetKey = string.Empty;

    [DataField]
    public string LatchKey = string.Empty;

    /// <summary>
    ///     Where the pathfinding result will be stored (if applicable). This gets removed after execution.
    /// </summary>
    [DataField]
    public string PathfindKey = NPCBlackboard.PathfindKey;

    [DataField]
    public SatiationValue Peckish = 25;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
        _mobSystem = sysManager.GetEntitySystem<MobStateSystem>();
        _factions = sysManager.GetEntitySystem<NpcFactionSystem>();
        _satiation = sysManager.GetEntitySystem<SatiationSystem>();
        _latch = sysManager.GetEntitySystem<SlimeLatchSystem>();

        _latchedQuery = _ent.GetEntityQuery<BeingLatchedComponent>();
        _satiationQuery = _ent.GetEntityQuery<SatiationComponent>();
        _dotQuery = _ent.GetEntityQuery<SlimeDamageOvertimeComponent>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<float>(RangeKey, out var range, _ent)
        || !_ent.TryGetComponent<SlimeComponent>(owner, out var slimeComp)
        || !_ent.TryGetComponent<MobGrowthComponent>(owner, out var growthComp)
        || _latch.IsLatched((owner, slimeComp)))
            return (false, null);

        // TODO: cache this slop
        var satiation = _satiationQuery.Comp(owner);
        var full = _satiation.IsValueInRange((owner, satiation), SatiationSystem.Hunger, above: Peckish);
        if (growthComp.IsFirstStage && full)
            return (false, null);

        var targets = new ValueList<EntityUid>();
        foreach (var entity in _factions.GetNearbyHostiles(owner, range))
        {
            if (_latchedQuery.HasComp(entity) ||
                _dotQuery.HasComp(entity) || // it's taken
                _mobSystem.IsDead(entity) ||
                (entity == slimeComp.Tamer && (full || growthComp.IsFirstStage))) // no killing tamer unless hungry and grown
                continue;

            targets.Add(entity);
        }

        foreach (var target in targets)
        {
            if (!_ent.TryGetComponent<TransformComponent>(target, out var xform))
                continue;

            var targetCoords = xform.Coordinates;
            var path = await _pathfinding.GetPath(owner, target, range, cancelToken);

            if (path.Result != PathResult.Path)
                continue;

            return (true, new Dictionary<string, object>()
            {
                { TargetKey, targetCoords },
                { LatchKey, target },
                { PathfindKey, path },
            });
        }

        return (false, null);
    }
}
