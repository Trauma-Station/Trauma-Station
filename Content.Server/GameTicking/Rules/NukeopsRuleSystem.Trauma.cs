// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking.Rules.Components;
using Content.Shared.NukeOps;
using Content.Trauma.Common.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed partial class NukeopsRuleSystem
{
    [Dependency] private IConfigurationManager _cfg = default!;

    private EntProtoId _newAntag = "ModerateAntagEventScheduler";
    private void InitializeTrauma()
    {
        Subs.CVar(_cfg, TraumaCVars.NukiePercentNeededForNewAntag, x => _percentNeededForNewAntag = x, true);
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<NukeOperativeComponent> ent, ref MapInitEvent args)
    {
        var query = EntityQueryEnumerator<NukeopsRuleComponent>();

        while (query.MoveNext(out _, out var comp))
        {
            comp.AmountAliveOnSpawn = _target.GetAliveHumans().Count;
            break;
        }
    }
}
