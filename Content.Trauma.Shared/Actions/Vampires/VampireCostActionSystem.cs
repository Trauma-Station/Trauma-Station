// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions.Events;
using Content.Shared.Popups;
using Content.Trauma.Shared.Vampires;

namespace Content.Trauma.Shared.Actions.Vampires;

public sealed class VampireCostActionSystem : EntitySystem
{
    [Dependency] private readonly VampireSystem _vampire = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireCostActionComponent, ActionPerformedEvent>(OnPerform);
        SubscribeLocalEvent<VampireCostActionComponent, ActionAttemptEvent>(OnAttempt);
    }

    private void OnPerform(Entity<VampireCostActionComponent> ent, ref ActionPerformedEvent args)
    {
        _vampire.SubtractUsableBlood(args.Performer, ent.Comp.BloodCost);
    }

    private void OnAttempt(Entity<VampireCostActionComponent> ent, ref ActionAttemptEvent args)
    {
        if (_vampire.HasUsableBlood(args.User, ent.Comp.BloodCost))
            return;

        _popup.PopupClient("You do not have enough usable blood to run this action!", args.User, args.User, PopupType.MediumCaution);
        args.Cancelled = true;
    }
}
