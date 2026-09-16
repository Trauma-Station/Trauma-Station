// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking.Rules;
using Content.Server.Revolutionary.Components;
using Content.Shared.Antag;
using Content.Trauma.Shared.Revolutionary;
using Robust.Shared.Player;

namespace Content.Trauma.Server.Revolutionary;

public sealed partial class RevConversionSystem : EntitySystem
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private ServerRevolutionaryRuleSystem _rev = default!;

    [SubscribeLocalEvent]
    private void OnRevConverted(ref RevConvertedEvent args)
    {
        if (TryComp<ActorComponent>(args.Target, out var actor))
            _antag.SendBriefing(actor.PlayerSession, Loc.GetString("rev-role-greeting"), Color.Red, args.Target.Comp.RevStartSound);

        if (!TryComp<CommandStaffComponent>(args.Target, out var command))
            return;

        command.Enabled = false;
        _rev.CheckCommandLose();
    }
}
