// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions.Events;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.RetractableItemAction;
using Content.Trauma.Shared.Vampires;

namespace Content.Trauma.Shared.Actions.Vampires;

public sealed partial class VampireCostActionSystem : EntitySystem
{
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private VampireSystem _vampire = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireCostActionComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<VampireCostActionComponent, ActionPerformedEvent>(OnPerform);
        SubscribeLocalEvent<VampireCostActionComponent, ActionAttemptEvent>(OnAttempt);

        SubscribeLocalEvent<RetractableItemActionComponent, VampireCostActionAttemptEvent>(OnRetractableAttempt);
    }

    private void OnMapInit(Entity<VampireCostActionComponent> ent, ref MapInitEvent args)
    {
        _meta.SetEntityDescription(ent, $"{Description(ent)} [color=red]{ent.Comp.BloodCost} Blood[/color]");
    }

    private void OnPerform(Entity<VampireCostActionComponent> ent, ref ActionPerformedEvent args)
    {
        var attemptEv = new VampireCostActionAttemptEvent(args.Performer);
        RaiseLocalEvent(ent, ref attemptEv);
        if (attemptEv.Cancelled)
            return;

        _vampire.SubtractUsableBlood(args.Performer, ent.Comp.BloodCost);
    }

    private void OnAttempt(Entity<VampireCostActionComponent> ent, ref ActionAttemptEvent args)
    {
        if (_vampire.HasUsableBlood(args.User, ent.Comp.BloodCost))
            return;

        _popup.PopupEntity(ent.Comp.Popup,
            args.User,
            args.User,
            PopupType.MediumCaution);

        args.Cancelled = true;
    }

    private void OnRetractableAttempt(Entity<RetractableItemActionComponent> ent, ref VampireCostActionAttemptEvent args)
    {
        // Handles the case where we press the retractable action, but we already have the weapon in our hand.
        // Retracting the existing item should not waste our usable blood.
        if (ent.Comp.ActionItemUid is not { } item|| _hands.IsHolding(args.Performer, item))
            return;

        args.Cancelled = true;
    }
}

/// <summary>
/// Raised on the action before subtracting blood from the user.
/// </summary>
[ByRefEvent]
public record struct VampireCostActionAttemptEvent(EntityUid Performer, bool Cancelled = false);
