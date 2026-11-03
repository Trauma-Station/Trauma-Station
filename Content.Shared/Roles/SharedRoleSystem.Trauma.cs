using Content.Shared.Mind;
using Robust.Shared.Network;

namespace Content.Shared.Roles;

public abstract partial class SharedRoleSystem
{
    [Dependency] private INetManager _net = default!;

    /// <summary>
    /// Removes all roles from a mind.
    /// </summary>
    public void MindClearRoles(Entity<MindComponent?> mind)
    {
        if (!Resolve(mind, ref mind.Comp) || mind.Comp.MindRoleContainer.Count < 1)
            return;

        var delete = new List<EntityUid>(mind.Comp.MindRoleContainer.ContainedEntities);
        MindRemoveRoleDo(mind, delete);
    }
}
