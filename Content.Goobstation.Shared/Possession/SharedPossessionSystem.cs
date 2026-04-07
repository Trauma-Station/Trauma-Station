// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Religion;
using Content.Goobstation.Shared.Devil;
using Content.Goobstation.Shared.Religion;
using Content.Goobstation.Shared.Religion.Nullrod;
using Content.Goobstation.Shared.Shadowling.Components;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Mindshield.Components;
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

        SubscribeLocalEvent<PossessionImmuneComponent, BeforeMindPossessEvent>(OnPossessImmune);
        SubscribeLocalEvent<MindShieldComponent, BeforeMindPossessEvent>(OnPossessMindShield);
        SubscribeLocalEvent<BibleUserComponent, BeforeMindPossessEvent>(OnPossessMindChaplain);
        SubscribeLocalEvent<PossessedComponent, BeforeMindPossessEvent>(OnPossessMindPossessed);
        SubscribeLocalEvent<ShadowlingComponent, BeforeMindPossessEvent>(OnPossessMindShadowling);
        SubscribeLocalEvent<DevilComponent, BeforeMindPossessEvent>(OnPossessMindDevil);
    }

    private void OnShouldSmite(Entity<PossessedComponent> ent, ref BibleSmiteAttemptEvent args)
    {
        args.ShouldSmite = true;
    }

    private void OnShouldTakeHoly(Entity<PossessedComponent> ent, ref UserShouldTakeHolyEvent args)
    {
        if (ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        args.WeakToHoly = true;
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

    // Check for possession immunity (e.g., tinfoil hat)
    private void OnPossessImmune(Entity<PossessionImmuneComponent> ent, ref BeforeMindPossessEvent args)
    {
        if (args.Cancelled || !args.DoesImmuneBlock)
            return;

        args.Message = "immune";
        args.Cancelled = true;
    }

    private void OnPossessMindShield(Entity<MindShieldComponent> ent, ref BeforeMindPossessEvent args)
    {
        if (args.Cancelled || !args.DoesMindshieldBlock)
            return;

        args.Message = "shielded";
        args.Cancelled = true;
    }

    private void OnPossessMindChaplain(Entity<BibleUserComponent> ent, ref BeforeMindPossessEvent args)
    {
        if (args.Cancelled || !args.DoesChaplainBlock)
            return;
        args.Message = "chaplain";
        args.Cancelled = true;
    }

    private void OnPossessMindPossessed(Entity<PossessedComponent> ent, ref BeforeMindPossessEvent args)
    {
        if (args.Cancelled)
            return;

        args.Message = "already-possessed";
        args.Cancelled = true;
    }

    private void OnPossessMindShadowling(Entity<ShadowlingComponent> ent, ref BeforeMindPossessEvent args)
    {
        if (args.Cancelled)
            return;

        args.Message = "shadowling";
        args.Cancelled = true;
    }

    private void OnPossessMindDevil(Entity<DevilComponent> ent, ref BeforeMindPossessEvent args)
    {
        if (args.Cancelled)
            return;

        args.Message = "devil";
        args.Cancelled = true;
    }

    protected virtual void PossessionEnded(Entity<PossessedComponent> possessed)
    {
        // server-side for using original entity and polymorph
    }
}

[Serializable, NetSerializable]
public record struct BeforeMindPossessEvent(bool DoesImmuneBlock, bool DoesMindshieldBlock, bool DoesChaplainBlock, bool Cancelled = false, string Message = "");
