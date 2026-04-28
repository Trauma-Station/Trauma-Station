// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Cuffs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Server.HTN.PrimitiveTasks.Operators.Specific;

public sealed partial class ForceHandcuffOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    private SharedCuffableSystem _cuffable = default!;
    private SharedAudioSystem _audio = default!;

    [DataField(required: true)]
    public string TargetKey = string.Empty;

    [DataField(required: true)]
    public string HandcuffKey = string.Empty;

    [DataField]
    public string? TargetArrestedSoundKey;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _cuffable = sysManager.GetEntitySystem<SharedCuffableSystem>();
        _audio = sysManager.GetEntitySystem<SharedAudioSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entMan) || _entMan.Deleted(target))
            return HTNOperatorStatus.Failed;

        if (!blackboard.TryGetValue<EntityUid>(HandcuffKey, out var handcuff, _entMan) || _entMan.Deleted(handcuff))
            return HTNOperatorStatus.Failed;

        if (!_cuffable.TryCuffing(owner, target, handcuff))
            return HTNOperatorStatus.Failed;

        if (TargetArrestedSoundKey != null && blackboard.TryGetValue<SoundSpecifier>(TargetArrestedSoundKey, out var targetArrestedSound, _entMan))
            _audio.PlayPvs(targetArrestedSound, owner);

        return HTNOperatorStatus.Finished;
    }
}
