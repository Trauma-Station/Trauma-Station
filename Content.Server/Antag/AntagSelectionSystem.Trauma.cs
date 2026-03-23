// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Server.Antag.Components;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Antag;

/// <summary>
/// Trauma - various api additions
/// </summary>
public sealed partial class AntagSelectionSystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public void UnequipOldGear(EntityUid player)
    {
        if (!TryComp<InventoryComponent>(player, out var comp))
            return;

        foreach (var slot in comp.Slots)
        {
            _inventory.TryUnequip(player, slot.Name, true, true, inventory: comp);
        }
    }

    public List<ICommonSession> GetAliveConnectedPlayers(IList<ICommonSession> pool)
    {
        var l = new List<ICommonSession>();
        foreach (var session in pool)
        {
            if (session.Status is SessionStatus.Disconnected or SessionStatus.Zombie)
                continue;
            l.Add(session);
        }
        return l;
    }

    /// <summary>
    /// Get all definition blacklists from sessions that have been preselected for antag. | GOOBSTATION
    /// </summary>
    public Dictionary<ICommonSession, List<ProtoId<JobPrototype>>> GetPreSelectedAntagSessionsWithBlacklist(AntagSelectionDefinition? except = null)
    {
        var result = new Dictionary<ICommonSession, List<ProtoId<JobPrototype>>>();
        var query = QueryAllRules();

        while (query.MoveNext(out var uid, out var comp, out _))
        {
            if (HasComp<EndedGameRuleComponent>(uid))
                continue;

            foreach (var def in comp.Definitions)
            {
                if (def.Equals(except) || !comp.PreSelectedSessions.TryGetValue(def, out var sessions))
                    continue;

                foreach (var session in sessions)
                {
                    // Get the blacklisted jobs for this antag definition
                    var blacklist = def.JobBlacklist ?? new List<ProtoId<JobPrototype>>();

                    // If session already exists, merge the blacklists
                    if (result.TryGetValue(session, out var existingBlacklist))
                    {
                        existingBlacklist.AddRange(blacklist);
                    }
                    else
                    {
                        result[session] = new List<ProtoId<JobPrototype>>(blacklist);
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Type-erased ForceMakeAntag overload
    /// </summary>
    public void ForceMakeAntag(ICommonSession? player, [ForbidLiteral] EntProtoId defaultRule, [ForbidLiteral] string comp)
    {
        var rule = ForceGetGameRuleEnt(defaultRule, comp);

        if (!TryGetNextAvailableDefinition(rule, out var def))
            def = rule.Comp.Definitions.Last();
        MakeAntag(rule, player, def.Value);
    }

    /// <summary>
    /// Type-erased ForceGetGameRuleEnt overload
    /// </summary>
    public Entity<AntagSelectionComponent> ForceGetGameRuleEnt([ForbidLiteral] EntProtoId id, [ForbidLiteral] string comp)
    {
        var type = Factory.GetRegistration(comp).Type;
        var query = EntityManager.AllEntityQueryEnumerator(type);
        while (query.MoveNext(out var uid, out _))
        {
            if (TryComp<AntagSelectionComponent>(uid, out var ontag))
                return (uid, ontag);
        }

        var ruleEnt = GameTicker.AddGameRule(id);
        RemComp<LoadMapRuleComponent>(ruleEnt);
        var antag = Comp<AntagSelectionComponent>(ruleEnt);
        antag.AssignmentComplete = true; // don't do normal selection.
        GameTicker.StartGameRule(ruleEnt);
        return (ruleEnt, antag);
    }
}
