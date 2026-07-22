// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Ghost.Roles.Components;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Shared.Ghost.Roles.Components;
using Content.Trauma.Common.Construction;
using Content.Trauma.Shared.Familiar;
using Content.Trauma.Shared.Spawners;
using Robust.Shared.Random;

namespace Content.Trauma.Server.Spawners;

public sealed partial class RandomDemonSpawnerSystem : EntitySystem
{
    [Dependency] private FamiliarSystem _familiar = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SpawnOnDespawnSystem _spawnOnDespawn = default!;

    private static readonly EntProtoId FamiliarRole = "MindRoleGhostRoleFamiliar";

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<RandomDemonSpawnerComponent> ent, ref MapInitEvent args)
    {
        var demon = _random.Pick(ent.Comp.Demons);
        EnsureComp<GhostRoleMobSpawnerComponent>(ent).Prototype = demon; // this one can be a familiar
        var despawn = EnsureComp<SpawnOnDespawnComponent>(ent);
        _spawnOnDespawn.SetPrototype((ent, despawn), demon); // it will be hostile if the timer expires
    }

    [SubscribeLocalEvent]
    private void OnConstructionChanged(Entity<RandomDemonSpawnerComponent> ent, ref ConstructionChangedEvent args)
    {
        if (ent.Owner == args.Old ||
            args.User is not { } user ||
            _random.Prob(ent.Comp.HostileChance))
            return;

        var role = Comp<GhostRoleComponent>(ent);
        role.RoleName = "ghost-role-information-demon-tame-name";
        role.RoleDescription = "ghost-role-information-demon-tame-desc";
        role.RoleRules = "ghost-role-information-familiar-rules";
        role.MindRoles.Clear();
        role.MindRoles.Add(FamiliarRole);

        _familiar.SetMaster(ent.Owner, user); // will update the spawned demon as well when a ghost takes it
    }
}
