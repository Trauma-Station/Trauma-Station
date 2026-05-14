// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;

namespace Content.Trauma.Server.HTN.Preconditions;

public sealed partial class KeyExistsPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    /// <summary>
    /// The blackboard key we are checking for.
    /// </summary>
    [DataField(required: true)]
    public string Key = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<object>(Key, out var value, _entManager))
            return false;

        if (value is not EntityUid entity)
            return false;

        return _entManager.EntityExists(entity) && !_entManager.Deleted(entity);
    }
}
