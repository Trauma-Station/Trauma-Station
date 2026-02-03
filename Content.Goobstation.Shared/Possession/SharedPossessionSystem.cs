// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Goobstation.Shared.Religion;
using Content.Goobstation.Shared.Religion.Nullrod;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Possession;

public abstract class SharedPossessionSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] protected readonly SharedContainerSystem _container = default!;
    [Dependency] protected readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PossessedComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PossessedComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PossessedComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PossessedComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<PossessedComponent, UserShouldTakeHolyEvent>(OnShouldTakeHoly);
        SubscribeLocalEvent<PossessedComponent, BibleSmiteAttemptEvent>(OnShouldSmite);
    }

    private void OnShouldSmite(Entity<PossessedComponent> ent, ref BibleSmiteAttemptEvent args)
    {
        args.ShouldSmite = true;
    }

    private void OnShouldTakeHoly(Entity<PossessedComponent> ent, ref UserShouldTakeHolyEvent args)
    {
        if (ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        args.ShouldTakeHoly = true;
    }

    private void OnInit(Entity<PossessedComponent> possessed, ref ComponentInit args)
    {
        possessed.Comp.PossessedContainer = _container.EnsureContainer<Container>(possessed, "PossessedContainer");
    }

    private void OnMapInit(Entity<PossessedComponent> possessed, ref MapInitEvent args)
    {
        EnsureComp<WeakToHolyComponent>(possessed);
        var ev = new UnholyStatusChangedEvent(possessed, possessed, true);
        RaiseLocalEvent(possessed, ref ev);

        if (possessed.Comp.HideActions)
            possessed.Comp.HiddenActions = _actions.HideActions(possessed);

        _actions.AddAction(possessed, ref possessed.Comp.ActionEntity, possessed.Comp.EndPossessionAction);
    }

    private void OnExamined(Entity<PossessedComponent> possessed, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange
            || args.Examined != args.Examiner)
            return;

        var remaining = possessed.Comp.PossessionEndTime - _timing.CurTime;
        var timeRemaining = Math.Floor(remaining.TotalSeconds);
        args.PushMarkup(Loc.GetString("possessed-component-examined", ("timeremaining", timeRemaining)));
    }

    private void OnRemove(Entity<PossessedComponent> possessed, ref ComponentRemove args)
    {
        _actions.RemoveAction(possessed.Owner, possessed.Comp.ActionEntity);

        if (possessed.Comp.HideActions)
            _actions.UnHideActions(possessed, possessed.Comp.HiddenActions);

        // Paralyze, so you can't just magdump them.
        _stun.TryAddParalyzeDuration(possessed, TimeSpan.FromSeconds(2));
        _popup.PopupClient(Loc.GetString("possession-end-popup", ("target", possessed)), possessed, possessed, PopupType.LargeCaution);

        PossessionEnded(possessed);
    }

    protected virtual void PossessionEnded(Entity<PossessedComponent> possessed)
    {
        // server-side for using original entity and polymorph
    }
}
