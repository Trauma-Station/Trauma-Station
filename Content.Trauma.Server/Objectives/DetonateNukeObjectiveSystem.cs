// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.Objectives;
using Content.Server.GameTicking.Rules;
using Content.Server.Nuke;
using Content.Shared.Station.Components;
using Content.Shared.GameTicking;
using Content.Shared.Objectives.Components;
using Content.Trauma.Server.GameTicking.Rules;

namespace Content.Trauma.Server.Objectives;

public sealed partial class DetonateNukeObjectiveSystem : EntitySystem
{
    private bool _stationNuked;

    [SubscribeLocalEvent(before: [typeof(XenomorphsRuleSystem), typeof(NukeopsRuleSystem)])]
    private void OnNuke(NukeExplodedEvent ev)
    {
        if (HasComp<BecomesStationComponent>(ev.OwningStation))
            _stationNuked = true;
    }

    [SubscribeLocalEvent]
    private void OnRestart(RoundRestartCleanupEvent ev)
    {
        _stationNuked = false;
    }

    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<DetonateNukeConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = _stationNuked ? 1f : 0f;
    }
}
