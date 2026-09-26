// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.Devil.Objectives.Components;
using Content.Goobstation.Shared.Devil;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Trauma.Shared.AER;
using Robust.Server.Player;

namespace Content.Trauma.Server.AER;

public sealed partial class AerMarksmanSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IPlayerManager _player = default!;

    [SubscribeLocalEvent]
    private void OnSoulAmountChanged(Entity<AerMarksmanComponent> devil, ref SoulAmountChangedEvent args)
    {
        if (!_mind.TryGetMind(args.User, out var mindId, out var mind))
            return;

        devil.Comp.Souls += args.Amount;
        _popup.PopupEntity(Loc.GetString("contract-soul-added"), args.User, args.User, PopupType.MediumCaution);


        if (_mind.TryGetObjectiveComp<SignContractConditionComponent>(mindId, out var objectiveComp, mind))
            objectiveComp.ContractsSigned += args.Amount;
    }


    //adding objectives on mind added message i'm not super sure this is right but it worked, stolen from autotraitor
    [SubscribeLocalEvent]
    private void OnMindAdded(Entity<AerMarksmanComponent> ent, ref MindAddedMessage args)
    {
        if (!_player.TryGetSessionById(args.Mind.Comp.UserId, out var session))
            return;
        _mind.TryAddObjective(ent, args.Mind.Comp, "AerMarksmanContractObjective");
        _mind.TryAddObjective(ent, args.Mind.Comp, "AerBreachObjective");
        _mind.TryAddObjective(ent, args.Mind.Comp, "AerMarksmanLarpObjective");
    }
}
