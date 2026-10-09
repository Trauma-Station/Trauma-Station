// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Antag.Components;
using Content.Shared.EntityEffects;
using Content.Shared.GameTicking.Components;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.Antag;

/// <summary>
/// Trauma - various api additions
/// </summary>
public abstract partial class AntagSelectionSystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private EntityQuery<AntagSelectionComponent> _query = default!;

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
    /// Type-erased ForceMakeAntag overload
    /// </summary>
    public void ForceMakeAntag(ICommonSession player, [ForbidLiteral] EntProtoId defaultRule, [ForbidLiteral] CompName comp)
    {
        if (ForceGetGameRuleEnt(defaultRule, comp) is not { } rule ||
            TryAssignNextAvailableAntag(rule, player, checkPref: false) ||
            rule.Comp.Antags.LastOrDefault() is not { } antag ||
            !ProtoMan.Resolve(antag.Proto, out var proto))
            return;

        PreSelectSession(rule, proto, player);
        TryInitializeAntag(rule, proto, player);
    }

    /// <summary>
    /// Type-erased ForceGetGameRuleEnt overload
    /// </summary>
    public virtual Entity<AntagSelectionComponent>? ForceGetGameRuleEnt([ForbidLiteral] EntProtoId id, [ForbidLiteral] CompName comp)
        => null;

    /// <summary>
    /// Find the first antag gamerule with a given component.
    /// </summary>
    public Entity<AntagSelectionComponent>? FindRule([ForbidLiteral] CompName comp)
    {
        var type = Factory.GetRegistration(comp).Type;
        var query = EntityManager.AllEntityQueryEnumerator(type);
        while (query.MoveNext(out var uid, out _))
        {
            if (_query.TryComp(uid, out var ontag))
                return (uid, ontag);
        }

        return null;
    }

    /// <summary>
    /// Returns true if a player was selected for any antag from a gamerule.
    /// </summary>
    public bool IsPlayerAnyAntag(Entity<AntagSelectionComponent> rule, ICommonSession player)
    {
        foreach (var players in rule.Comp.PreSelectedSessions.Values)
        {
            if (players.Contains(player))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Forces a player to become a specific antag of a gamerule, ignoring its limits.
    /// </summary>
    public void ForceMakeAntag(ICommonSession player, Entity<AntagSelectionComponent> rule, AntagSpecifierPrototype specifier)
    {
        PreSelectSession(rule, specifier, player);
        TryInitializeAntag(rule, specifier, player);
    }
}
