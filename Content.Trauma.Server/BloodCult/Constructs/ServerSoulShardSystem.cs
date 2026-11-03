// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Ghost.Roles.Components;
using Content.Trauma.Shared.BloodCult.Constructs.SoulShard;

namespace Content.Trauma.Server.BloodCult.Constructs;

public sealed partial class ServerSoulShardSystem : SoulShardSystem
{
    protected override void PurifyGhostRole(EntityUid uid)
    {
        if (!TryComp<GhostRoleComponent>(uid, out var role))
            return;

        role.RoleName = "ghost-role-information-purified-soul-shard-name";
        role.RoleDescription = "ghost-role-information-purified-soul-shard-description";
        role.RoleRules = "ghost-role-information-purified-soul-shard-rules";
    }
}
