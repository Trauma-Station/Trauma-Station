// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Lavaland.Shared.Megafauna.Systems;
using Content.Shared.Actions;

namespace Content.Lavaland.Shared.Megafauna.Conditions;

public sealed partial class ActionAvailableCondition : MegafaunaCondition
{
    [DataField(required: true)]
    public EntProtoId ActionId;

    public override bool EvaluateImplementation(MegafaunaCalculationBaseArgs args)
    {
        var entMan = args.EntMan;
        var actions = entMan.System<SharedActionsSystem>();

        if (!actions.TryGetActionById(args.Entity, ActionId, out var action))
            return false;

        var ev = args.System.GetPerformEvent(args.Entity, action.Value.Owner);
        return actions.CanPerformAction(args.Entity, action.Value, ev);
    }
}
