// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.GameTicking.Rules.Components;
using Content.Shared.Roles.Components;
using Content.Shared.Tag;
using Content.Trauma.Common.GameTicking;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed partial class ServerNukeopsRuleSystem
{
    [Dependency] private CommonNewAntagOrEvacSystem _antagEvac = default!;

    private static readonly ProtoId<TagPrototype> NukeOpsReinforcementUplinkTagPrototype = "NukeOpsReinforcementUplink";

    private int CalculateBonusTcPerNukie(NukeopsRuleComponent rule)
    {
        var nukiesCount = Count<NukeopsRoleComponent>();
        if (nukiesCount == 0)
            return rule.WarTcAmountPerNukie;

        var totalPlayersCount = 0;
        foreach (var session in _player.Sessions)
        {
            if (!Antag.IsDisconnected(session))
                totalPlayersCount++;
        }
        var playersCount = Math.Max(0, totalPlayersCount - nukiesCount);
        var maxNukies = totalPlayersCount / rule.WarNukiePlayerRatio;
        var nukiesMissing = Math.Max(0, maxNukies - nukiesCount);
        var totalBonus = playersCount * rule.WarTcPerPlayer;
        totalBonus += nukiesMissing * rule.WarTcPerNukieMissing;
        return Math.Max(rule.WarTcAmountPerNukie, totalBonus / nukiesCount);
    }
}
