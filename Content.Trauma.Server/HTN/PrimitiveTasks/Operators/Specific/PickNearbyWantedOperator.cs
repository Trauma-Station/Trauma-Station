using Content.Server.NPC.Pathfinding;
using Content.Shared.Cuffs.Components;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Security.Components;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Coordinates;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Server.HTN.PrimitiveTasks.Operators.Specific;

[DataDefinition]
public sealed partial class PickNearbyWantedOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private EntityLookupSystem _lookup = default!;
    private PathfindingSystem _pathfinding = default!;
    private SharedAudioSystem _audio = default!;

    [DataField]
    public string RangeKey = NPCBlackboard.SecuritronArrestRange;

    /// <summary>
    /// Target entity to inject
    /// </summary>
    [DataField(required: true)]
    public string TargetKey = string.Empty;

    /// <summary>
    /// Target entitycoordinates to move to.
    /// </summary>
    [DataField(required: true)]
    public string TargetMoveKey = string.Empty;

    /// <summary>
    /// The criminal status the target has to be for it to be a target
    /// </summary>
    [DataField(required: true)]
    public ProtoId<SecurityIconPrototype> CriminalStatus;

    /// <summary>
    /// The sound to play when it finds a target
    /// </summary>
    [DataField]
    public SoundCollectionSpecifier? TargetFoundSound;

    private HashSet<Entity<CriminalRecordComponent>> _entities = new();

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _lookup = sysManager.GetEntitySystem<EntityLookupSystem>();
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
        _audio = sysManager.GetEntitySystem<SharedAudioSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<float>(RangeKey, out var range, _entManager))
            return (false, null);

        var cuffableQuery = _entManager.GetEntityQuery<CuffableComponent>();
        var mobStateQuery = _entManager.GetEntityQuery<MobStateComponent>();

        _entities.Clear();
        _lookup.GetEntitiesInRange(owner.ToCoordinates(), range, _entities);

        foreach (var entity in _entities)
        {
            if (entity.Comp.StatusIcon != CriminalStatus)
                continue;

            if (!mobStateQuery.TryGetComponent(entity, out var state) || state.CurrentState != MobState.Alive)
                continue;

            // we still want to stun them if they cant ever be fully arrested
            if (cuffableQuery.TryGetComponent(entity, out var cuffable) && cuffable.CuffedHandCount > 0)
                continue;

            //Needed to make sure it doesn't sometimes stop right outside it's interaction range
            var pathRange = SharedInteractionSystem.InteractionRange - 1f;
            var path = await _pathfinding.GetPath(owner, entity, pathRange, cancelToken);

            if (path.Result != PathResult.Path)
                continue;

            if (TargetFoundSound != null &&
                (!blackboard.TryGetValue<EntityUid>(TargetKey, out var oldTarget, _entManager) ||
                 oldTarget != entity.Owner))
            {
                var targetFoundSound = _audio.ResolveSound(TargetFoundSound);
                _audio.PlayPvs(targetFoundSound, owner);
            }

            return (true, new Dictionary<string, object>()
            {
                {TargetKey, entity.Owner},
                {TargetMoveKey, _entManager.GetComponent<TransformComponent>(entity).Coordinates},
                {NPCBlackboard.PathfindKey, path},
            });
        }

        return (false, null);
    }
}
