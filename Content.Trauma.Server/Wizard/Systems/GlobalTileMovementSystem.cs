// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Ghost.Roles.Events;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Trauma.Server.Wizard.Systems;
using Content.Trauma.Shared.Wizard;
using Content.Trauma.Shared.Wizard.EventSpells;
using Robust.Server.Audio;
using Robust.Shared.Player;

namespace Content.Trauma.Server.Wizard.Systems;

public sealed partial class GlobalTileMovementSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private IAdminLogManager _log = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private WizardRuleSystem _wizardRuleSystem = default!;
    private static readonly EntProtoId GameRule = "GlobalTileMovement";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GlobalTileToggleEvent>(OnGlobalTileToggle);
        SubscribeLocalEvent<GlobalTileMovementRuleComponent, GameRuleStartedEvent>(OnRuleStarted);
        SubscribeLocalEvent<GhostRoleSpawnerUsedEvent>(OnGhostRoleSpawnerUsed);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
    }

    public Entity<GlobalTileMovementRuleComponent>? GetRule()
    {
        var query = EntityQueryEnumerator<GlobalTileMovementRuleComponent, ActiveGameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp, out _))
        {
            return (uid, comp);
        }

        return null;
    }

    private void OnGlobalTileToggle(GlobalTileToggleEvent ev)
    {
        if (GetRule() != null)
            return;

        _gameTicker.StartGameRule(GameRule);

        var message = Loc.GetString("global-tile-movement-message");
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
        _chatManager.ChatMessageToAll(ChatChannel.Radio, message, wrappedMessage, default, false, true, Color.Red);
        _audio.PlayGlobal(ev.Sound, Filter.Broadcast(), true);
        _log.Add(LogType.EventRan, LogImpact.Extreme, $"Tile movement has been globally toggled via wizard spellbook.");
    }

    private void OnRuleStarted(Entity<GlobalTileMovementRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        var map = _wizardRuleSystem.GetTargetMap();

        if (map == null)
            return;

        var entities = new HashSet<Entity<MobStateComponent, MindContainerComponent>>();
        _lookup.GetEntitiesOnMap<MobStateComponent, MindContainerComponent>(Transform(map.Value).MapID, entities);
        foreach (var (uid, _, _) in entities)
        {
            if (TerminatingOrDeleted(uid))
                continue;

            EntityManager.AddComponents(uid, ent.Comp.Components);
        }
    }

    private void OnGhostRoleSpawnerUsed(GhostRoleSpawnerUsedEvent args)
    {
        if (GetRule() is not { } rule)
            return;

        EntityManager.AddComponents(args.Spawned, rule.Comp.Components);
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent ev)
    {
        if (GetRule() is not { } rule ||
            !ev.LateJoin ||
            TerminatingOrDeleted(ev.Mob))
            return;

        EntityManager.AddComponents(ev.Mob, rule.Comp.Components);
    }
}
