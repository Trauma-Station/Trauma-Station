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
            TryAssignNextAvailableAntag(rule, player) ||
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
    {
        return null;
    }
}
