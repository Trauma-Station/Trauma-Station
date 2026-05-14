// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;

[DataDefinition]
public sealed partial class SpawnHandcuffOperator : HTNOperator
{
    [Dependency] private IEntityManager _entMan = default!;

    [DataField(required: true)]
    public EntProtoId HandcuffPrototype = "Handcuffs";

    [DataField(required: true)]
    public string HandcuffKey = "HandcuffItem";

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        return (true, new Dictionary<string, object>
        {
            {HandcuffKey, EntityUid.Invalid},
            {NPCBlackboard.SecuritronArrestRange, 12f},
        });
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        // Spawn the cuffs at the bot's position
        var cuffs = _entMan.SpawnEntity(HandcuffPrototype, _entMan.GetComponent<TransformComponent>(owner).Coordinates);

        // Update the blackboard with the real UID
        blackboard.SetValue(HandcuffKey, cuffs);

        return HTNOperatorStatus.Finished;
    }
}
