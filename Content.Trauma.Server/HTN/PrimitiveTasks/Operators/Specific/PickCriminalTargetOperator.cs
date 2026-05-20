// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading;
using System.Threading.Tasks;
using Content.Goobstation.Shared.Contraband;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Access.Systems;
using Content.Shared.Coordinates;
using Content.Shared.Cuffs.Components;
using Content.Shared.Emag.Components;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Security.Components;
using Content.Shared.StatusIcon;
using Content.Shared.Stealth.Components;
using Content.Shared.Tag;
using Content.Trauma.Shared.Card;
using Robust.Server.Containers;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Server.HTN.PrimitiveTasks.Operators.Specific;

[DataDefinition]
public sealed partial class PickCriminalTargetOperator : HTNOperator
{
    [Dependency] private IEntityManager _entMan = default!;
    private TagSystem _tag = default!;
    private EntityLookupSystem _lookup = default!;
    private PathfindingSystem _pathfinding = default!;
    private SharedContrabandDetectorSystem _contra = default!;
    private SharedIdCardSystem _card = default!;
    private SharedAudioSystem _audio = default!;
    private ContainerSystem _container = default!;
    private EntityQuery<CuffableComponent> _cuffableQuery = default!;
    private EntityQuery<MobStateComponent> _mobQuery = default!;
    private EntityQuery<AntagCardComponent> _cardQuery = default!;
    private EntityQuery<CriminalRecordComponent> _criminalQuery = default!;
    private EntityQuery<EmaggedComponent> _emagQuery = default!;
    private EntityQuery<StealthComponent> _stealthQuery = default!;


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
    [DataField(required: true)]
    public SoundCollectionSpecifier TargetFoundSound;

    private HashSet<Entity<MobStateComponent>> _entities = new();
    private readonly static ProtoId<TagPrototype> BotTag = "Bot";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _tag = sysManager.GetEntitySystem<TagSystem>();
        _lookup = sysManager.GetEntitySystem<EntityLookupSystem>();
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
        _contra = sysManager.GetEntitySystem<SharedContrabandDetectorSystem>();
        _card = sysManager.GetEntitySystem<SharedIdCardSystem>();
        _audio = sysManager.GetEntitySystem<SharedAudioSystem>();
        _container = sysManager.GetEntitySystem<ContainerSystem>();

        _cuffableQuery = _entMan.GetEntityQuery<CuffableComponent>();
        _mobQuery = _entMan.GetEntityQuery<MobStateComponent>();
        _cardQuery = _entMan.GetEntityQuery<AntagCardComponent>();
        _criminalQuery = _entMan.GetEntityQuery<CriminalRecordComponent>();
        _emagQuery = _entMan.GetEntityQuery<EmaggedComponent>();
        _stealthQuery = _entMan.GetEntityQuery<StealthComponent>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var ownerCoords = owner.ToCoordinates();

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var targetEnt, _entMan))
            targetEnt = EntityUid.Invalid;

        var oldEntity = targetEnt;

        var range = 12f;

        _entities.Clear();

        bool isEmagged = _entMan.HasComponent<EmaggedComponent>(owner);
        MobStateComponent? mobState = null;

        if (targetEnt.Valid && _entMan.TryGetComponent<TransformComponent>(targetEnt, out var transformComp) && _mobQuery.Resolve(targetEnt, ref mobState))
        {
            if (!BeatUp((targetEnt, mobState), owner, isEmagged) || !ownerCoords.InRange(_entMan, transformComp.Coordinates, range))
                targetEnt = EntityUid.Invalid;
        }

        if (!targetEnt.Valid)
        {
            _lookup.GetEntitiesInRange(ownerCoords, range, _entities);
            foreach (var entity in _entities)
            {
                if (!BeatUp(entity, owner, isEmagged))
                    continue;

                targetEnt = entity;
                break;
            }
        }

        if (!targetEnt.Valid)
            return (false, null);

        var pathRange = SharedInteractionSystem.InteractionRange - 1f;
        var path = await _pathfinding.GetPath(owner, targetEnt, pathRange, cancelToken);

        if (path.Result != PathResult.Path)
            return (false, null);

        if (oldEntity != targetEnt)
            _audio.PlayPvs(TargetFoundSound, owner);

        return (true, new Dictionary<string, object>()
        {
            {TargetKey, targetEnt},
            {TargetMoveKey, _entMan.GetComponent<TransformComponent>(targetEnt).Coordinates},
            {NPCBlackboard.PathfindKey, path},
        });
    }

    private bool BeatUp(Entity<MobStateComponent> entity, EntityUid beepsky, bool isEmagged)
    {
        if (entity.Owner == beepsky)
            return false;

        if (_container.IsEntityInContainer(entity))
            return false;

        // Is target a living target?
        if (!_mobQuery.TryComp(entity, out var state) || state.CurrentState != MobState.Alive)
            return false;

        if (_stealthQuery.HasComp(entity))
            return false;

        bool isCriminal = (_criminalQuery.TryComp(entity, out var comp) || comp?.StatusIcon == CriminalStatus);
        bool hasContra = _contra.FindContraband(entity, false).Count > 0;
        bool isBadId = (!_card.TryFindIdCard(entity, out var idCard) || _cardQuery.HasComp(idCard)) && !(_tag.HasTag(entity, BotTag) && !_emagQuery.HasComp(entity));

        if (!isEmagged ^ (isCriminal || hasContra || isBadId))
            return false;

        // Is threat brought to order
        if (_cuffableQuery.TryComp(entity, out var cuffable) && cuffable.CuffedHandCount > 0)
            return false;

        return true;
    }
}
