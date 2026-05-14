// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;

namespace Content.Trauma.Server.HTN.Preconditions;

public sealed partial class TargetKeyExistsPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    /// <summary>
    /// The blackboard key we are checking for.
    /// </summary>
    [DataField(required: true)]
    public string Key = default!;

    /// <summary>
    /// Invert check.
    /// </summary>
    [DataField]
    public bool Invert = false;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var exists = blackboard.TryGetValue<EntityUid>(Key, out var entity, _entManager) && _entManager.EntityExists(entity) && !_entManager.Deleted(entity);

        return exists ^ Invert;
    }
}
