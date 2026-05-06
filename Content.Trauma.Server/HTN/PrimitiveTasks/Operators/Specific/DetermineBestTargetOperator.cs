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
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Security.Components;
using Content.Shared.StatusIcon;
using Content.Trauma.Shared.Card;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Server.HTN.PrimitiveTasks.Operators.Specific;

[DataDefinition]
public sealed partial class DetermineBestTargetOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    private EntityLookupSystem _lookup = default!;
    private PathfindingSystem _pathfinding = default!;
    private SharedContrabandDetectorSystem _contra = default!;
    private SharedIdCardSystem _card = default!;
    private SharedAudioSystem _audio = default!;
    private EntityQuery<CuffableComponent> _cuffableQuery = default!;
    private EntityQuery<MobStateComponent> _mobQuery = default!;
    private EntityQuery<AntagCardComponent> _cardQuery = default!;


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
        _contra = sysManager.GetEntitySystem<SharedContrabandDetectorSystem>();
        _card = sysManager.GetEntitySystem<SharedIdCardSystem>();
        _audio = sysManager.GetEntitySystem<SharedAudioSystem>();

        _cuffableQuery = _entMan.GetEntityQuery<CuffableComponent>();
        _mobQuery = _entMan.GetEntityQuery<MobStateComponent>();
        _cardQuery = _entMan.GetEntityQuery<AntagCardComponent>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var ownerCoords = owner.ToCoordinates();
        var targetLists = blackboard.GetValue<List<EntityUid>>(NPCBlackboard.TargetList);

        if (targetLists.Count <= 0)
            return (false, null);

        foreach (var entity in targetLists)
        {

        }
    }
}
