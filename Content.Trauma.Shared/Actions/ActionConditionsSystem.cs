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
        var user = args.User;
        args.Cancelled = ent.Comp.Any
            ? !_conditions.TryAnyCondition(user, ent.Comp.Conditions, user: user)
            : !_conditions.TryConditions(user, ent.Comp.Conditions, user: user);

        DoPopup(args.Cancelled, ent.Comp.FailPopup, user);
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
