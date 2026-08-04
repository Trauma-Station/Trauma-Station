// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Trauma.Server.GameTicking.Rules.Components;
using Content.Trauma.Shared.Magic.Demonologist.Components;

namespace Content.Trauma.Server.GameTicking.Rules;

public sealed partial class DemonologistRuleSystem : GameRuleSystem<DemonologistRuleComponent>
{

    [SubscribeLocalEvent]
    private void OnSelectAntag(EntityUid uid, DemonologistRuleComponent comp, ref AfterAntagEntitySelectedEvent args)
    {
        EnsureComp<DemonologistComponent>(args.EntityUid);
    }
}
