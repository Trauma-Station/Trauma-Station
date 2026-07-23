// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Objectives.Systems;
using Content.Shared.Revolutionary.Components;
using Content.Trauma.Common.CCVar;
using Content.Trauma.Common.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed partial class RevolutionaryRuleSystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private TargetSystem _target = default!;
    [Dependency] private CommonRequestNewAntagOrCallEvacSystem _antagEvacRequest = default!;

    private EntProtoId _newAntag = "ModerateAntagEventScheduler";
    private float _percentNeededForNewAntag;
    private int _amountAliveOnSpawn;

    private void InitializeTrauma()
    {
        Subs.CVar(_cfg, TraumaCVars.RevPercentNeededForNewAntag, x => _percentNeededForNewAntag = x, true);
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<HeadRevolutionaryComponent> ent, ref MapInitEvent args)
    {
        _amountAliveOnSpawn = _target.GetAliveHumans().Count;
    }
}
