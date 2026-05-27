// SPDX-License-Identifier: AGPL-3.0-or-later

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

namespace Content.Trauma.Server.HTN.PrimitiveTasks.Operators.Specific;

[DataDefinition]
public sealed partial class PickNearbyWantedOperator : HTNOperator
{
    [Dependency] private IEntityManager _entMan = default!;
    private EntityLookupSystem _lookup = default!;
    private PathfindingSystem _pathfinding = default!;
    private SharedAudioSystem _audio = default!;
    private EntityQuery<CuffableComponent> _cuffableQuery = default!;
    private EntityQuery<MobStateComponent> _mobQuery = default!;

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

        _cuffableQuery = _entMan.GetEntityQuery<CuffableComponent>();
        _mobQuery = _entMan.GetEntityQuery<MobStateComponent>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        var range = 12f;

        _entities.Clear();
        _lookup.GetEntitiesInRange(owner.ToCoordinates(), range, _entities);

        foreach (var entity in _entities)
        {
            if (entity.Comp.StatusIcon != CriminalStatus)
                continue;

            if (!_mobQuery.TryComp(entity, out var state) || state.CurrentState != MobState.Alive)
                continue;

            // we still want to stun them if they cant ever be fully arrested
            if (_cuffableQuery.TryComp(entity, out var cuffable) && cuffable.CuffedHandCount > 0)
                continue;

            //Needed to make sure it doesn't sometimes stop right outside it's interaction range
            var pathRange = SharedInteractionSystem.InteractionRange - 1f;
            var path = await _pathfinding.GetPath(owner, entity, pathRange, cancelToken);

            if (path.Result != PathResult.Path)
                continue;

            if (TargetFoundSound != null &&
                (!blackboard.TryGetValue<EntityUid>(TargetKey, out var oldTarget, _entMan) ||
                 oldTarget != entity.Owner))
            {
                var targetFoundSound = _audio.ResolveSound(TargetFoundSound);
                _audio.PlayPvs(targetFoundSound, owner);
            }

            return (true, new Dictionary<string, object>()
            {
                {TargetKey, entity.Owner},
                {TargetMoveKey, _entMan.GetComponent<TransformComponent>(entity).Coordinates},
                {NPCBlackboard.PathfindKey, path},
            });
        }

        return (false, null);
    }
}
