// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions.Events;
using Content.Shared.EntityConditions;
using Content.Shared.Popups;

namespace Content.Trauma.Shared.Actions;

public sealed partial class ActionConditionsSystem : EntitySystem
{
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionConditionsComponent, ActionAttemptEvent>(OnAttempt);
    }

    private void OnAttempt(Entity<ActionConditionsComponent> ent, ref ActionAttemptEvent args)
    {
        args.Cancelled = ent.Comp.Any
            ? !_conditions.TryAnyCondition(args.User, ent.Comp.Conditions)
            : !_conditions.TryConditions(args.User, ent.Comp.Conditions);

        DoPopup(args.Cancelled, ent.Comp.FailPopup, args.User);
    }

    #region  Helper
    /// <summary>
    /// Shows a popup to the user, if the conditions fail.
    /// </summary>
    private void DoPopup(bool passed, string popup, EntityUid user)
    {
        if (!passed)
            return;

        _popup.PopupClient(popup, user, user, PopupType.MediumCaution);
    }
    #endregion
}
