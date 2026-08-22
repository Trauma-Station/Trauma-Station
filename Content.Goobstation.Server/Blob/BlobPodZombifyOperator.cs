// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.Blob.NPC.BlobPod;
using Content.Goobstation.Shared.Blob.Components;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;

namespace Content.Goobstation.Server.Blob;

public sealed partial class BlobPodZombifyOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;
    private BlobPodSystem _blobPod = default!;

    [DataField(required: true)]
    public string ZombifyKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _blobPod = sysManager.GetEntitySystem<BlobPodSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var target = blackboard.GetValue<EntityUid>(ZombifyKey);

        if (!target.IsValid() || _entManager.Deleted(target))
            return HTNOperatorStatus.Failed;

        if (!_entManager.TryGetComponent<BlobPodComponent>(owner, out var pod))
            return HTNOperatorStatus.Failed;

        if (pod.ZombifiedEntityUid != null)
            return HTNOperatorStatus.Continuing;

        if (pod.IsZombifying)
            return HTNOperatorStatus.Continuing;

        if (pod.ZombifyTarget == null)
        {
            return _blobPod.NpcStartZombify((owner, pod), target)
                ? HTNOperatorStatus.Continuing
                : HTNOperatorStatus.Failed;
        }

        pod.ZombifyTarget = null;
        return HTNOperatorStatus.Finished;
    }
}
