// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Antag;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Systems;
using Content.Shared.Preferences;
using Content.Trauma.Shared.Ghost;
using Robust.Shared.Map;

namespace Content.Trauma.Server.Ghost;

/// <summary>
/// Handles ghost character dedicated spawners (reinforcements) and antag rules (e.g. ninja)
/// </summary>
public sealed partial class GhostCharacterSpawnerSystem : EntitySystem
{
    [Dependency] private GhostCharacterSystem _character = default!;
    [Dependency] private GhostRoleSystem _ghostRole = default!;
    [Dependency] private IServerPreferencesManager _prefs = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private StationSpawningSystem _spawning = default!;
    [Dependency] private EntityQuery<GhostRoleComponent> _roleQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostCharacterSpawnerComponent, TakeGhostRoleEvent>(OnTakeGhostRole);
        SubscribeLocalEvent<AntagGhostCharacterRuleComponent, AntagSelectEntityEvent>(OnSelectAntag);
    }

    private void OnTakeGhostRole(Entity<GhostCharacterSpawnerComponent> ent, ref TakeGhostRoleEvent args)
    {
        if (args.TookRole ||
            !_roleQuery.TryComp(ent, out var ghostRole) ||
            !_ghostRole.CanTakeGhost(ent, ghostRole))
            return;

        var coords = Transform(ent).Coordinates;
        var user = args.Player.UserId;
        var profile = GetDesiredProfile(user)
            ?? HumanoidCharacterProfile.RandomWithSpecies(ent.Comp.DefaultSpecies);

        // no using the same slot twice
        _character.AddSpawnedCharacter(user, profile.Name);
        _character.SendData(args.Player);

        var mob = _spawning.SpawnPlayerMob(coords, job: null, profile: profile, station: null);
        _transform.AttachToGridOrMap(mob);
        EntityManager.AddComponents(mob, ent.Comp.Components);

        var ev = new GhostRoleSpawnerUsedEvent(ent, mob);
        RaiseLocalEvent(mob, ev, true);

        _ghostRole.GhostRoleInternalCreateMindAndTransfer(args.Player, ent, mob, ghostRole);

        // TODO: add to station records if it's desired in the future

        args.TookRole = true;

        ghostRole.Taken = true;

        if (ent.Comp.DeleteOnSpawn)
            QueueDel(ent);
    }

    private void OnSelectAntag(Entity<AntagGhostCharacterRuleComponent> ent, ref AntagSelectEntityEvent args)
    {
        if (args.Handled)
            return;

        HumanoidCharacterProfile? profile = null;
        if (args.Session is {} session)
        {
            var user = session.UserId;
            profile = GetDesiredProfile(user);
            if (profile?.Name is {} name)
            {
                _character.AddSpawnedCharacter(user, name);
                _character.SendData(session);
            }
        }

        profile ??= HumanoidCharacterProfile.RandomWithSpecies(ent.Comp.DefaultSpecies);
        var coords = Transform(ent).Coordinates; // the gamerule happens to be in nullspace
        args.Entity = _spawning.SpawnPlayerMob(coords, job: null, profile: profile, station: null);
    }

    public HumanoidCharacterProfile? GetDesiredProfile(NetUserId user)
        => _character.GetGhostRoleSlot(user) is {} slot &&
            _prefs.TryGetCachedPreferences(user, out var prefs) &&
            prefs.Characters.TryGetValue(slot, out var profile)
            ? profile
            : null;
}
