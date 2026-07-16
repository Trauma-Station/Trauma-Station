// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.ActionBlocker;
using Content.Shared.Audio.Jukebox;
using Content.Shared.Buckle.Components;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Goobstation.Shared.Vehicles.Clowncar;

public sealed partial class ClowncarSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClowncarComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<ClowncarComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<ClowncarComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<ClowncarComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<ClowncarComponent, ThankRiderActionEvent>(OnThankRider);
        SubscribeLocalEvent<ClowncarComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<ClowncarComponent, ClownCarOpenTrunkDoAfterEvent>(OnOpenTrunk);
        SubscribeLocalEvent<ClowncarComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ClowncarComponent, QuietBackThereActionEvent>(OnQuietBackThere);
        SubscribeLocalEvent<ClowncarComponent, DrivingWithStyleActionEvent>(OnDrivingWithStyle);
    }

    /// <summary>
    /// Handles adding the "thank rider" action to passengers
    /// </summary>
    private void OnEntInserted(Entity<ClowncarComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.Container)
            return;

        EnsureComp<StunnedComponent>(args.Entity);
        _actions.AddAction(args.Entity, ent.Comp.ThankRiderAction, ent.Owner);
    }

    /// <summary>
    /// Adds actions to the driver.
    /// </summary>
    private void OnStrapped(Entity<ClowncarComponent> ent, ref StrappedEvent args)
    {
        var driver = args.Buckle.Owner;
        foreach (var id in ent.Comp.DriverActions)
        {
            _actions.AddAction(driver, id, ent.Owner);
        }
        ResetThankCounter(ent);
    }

    private void OnUnstrapped(Entity<ClowncarComponent> ent, ref UnstrappedEvent args)
    {
        _actions.RemoveProvidedActions(args.Buckle.Owner, ent.Owner);
    }

    /// <summary>
    /// Removes the thank rider action and unstuns passengers when removed.
    /// </summary>
    private void OnEntRemoved(Entity<ClowncarComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.Container)
            return;

        _actions.RemoveProvidedActions(args.Entity, ent.Owner);
        RemComp<StunnedComponent>(args.Entity);
    }

    private void OnThankRider(Entity<ClowncarComponent> ent, ref ThankRiderActionEvent args)
    {
        var user = args.Performer;
        if (args.Handled || !TryComp<VehicleComponent>(ent, out var vehicle) ||
            !_blocker.CanSpeak(user)) // mimes cant thank the driver...
            return;

        ent.Comp.ThankCounter++;
        Dirty(ent);

        if (vehicle.Driver is not { } driver)
        {
            _chat.TrySendInGameICMessage(user, Loc.GetString("clowncar-thank-no-driver"), InGameICChatType.Speak, false);
            args.Handled = true;

            if (_container.TryGetContainer(ent.Owner, ent.Comp.Container, out var container))
                _container.Remove(user, container);

            return;
        }

        var name = Identity.Name(driver, EntityManager);
        var message = Loc.GetString("clowncar-thank-driver", ("driver", name));
        _chat.TrySendInGameICMessage(user, message, InGameICChatType.Speak, false);
        args.Handled = true;

        if (ent.Comp.ThankCounter >= ent.Comp.FreedomThanks)
            OpenTrunk(ent); // freedom
    }

    private void OnGetVerbs(Entity<ClowncarComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;
        if (!args.CanInteract ||
            !_container.TryGetContainer(ent.Owner, ent.Comp.Container, out var container) ||
            container.Contains(user))
            return;

        args.Verbs.Add(new()
        {
            Text = "Open Trunk",
            Act = () => OpenTrunkVerb(ent, user)
        });
    }

    private void OpenTrunkVerb(Entity<ClowncarComponent> ent, EntityUid user)
    {
        var args =
        new DoAfterArgs(EntityManager, user, 5f, new ClownCarOpenTrunkDoAfterEvent(), ent, target: ent)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
        };
        _doAfter.TryStartDoAfter(args);
    }

    private void OnOpenTrunk(Entity<ClowncarComponent> ent, ref ClownCarOpenTrunkDoAfterEvent args)
    {
        if (!_container.TryGetContainer(ent.Owner, ent.Comp.Container, out var container) ||
            container.Contains(args.User))
            return;

        OpenTrunk(ent);
    }

    private void OpenTrunk(Entity<ClowncarComponent> ent)
    {
        if (!_container.TryGetContainer(ent.Owner, ent.Comp.Container, out var container))
            return;

        ResetThankCounter(ent);
        _container.EmptyContainer(container);
    }

    private void OnExamined(Entity<ClowncarComponent> ent, ref ExaminedEvent args)
    {
        if (!_container.TryGetContainer(ent.Owner, ent.Comp.Container, out var container))
            return;

        if (args.IsInDetailsRange)
            args.PushMarkup($"Contains {container.Count} Happy Passengers");
    }

    private void OnQuietBackThere(Entity<ClowncarComponent> ent, ref QuietBackThereActionEvent args)
    {
        ResetThankCounter(ent);
        _chat.TrySendInGameICMessage(args.Performer, Loc.GetString("clowncar-quiet-in-the-back"), InGameICChatType.Speak, false);
        args.Handled = true;
    }

    private void OnDrivingWithStyle(Entity<ClowncarComponent> ent, ref DrivingWithStyleActionEvent args)
    {
        _ui.TryOpenUi(ent.Owner, JukeboxUiKey.Key, args.Performer);
        args.Handled = true;
    }

    private void ResetThankCounter(Entity<ClowncarComponent> ent)
    {
        if (ent.Comp.ThankCounter == 0)
            return;

        ent.Comp.ThankCounter = 0;
        Dirty(ent);
    }
}

[Serializable, NetSerializable]
public sealed partial class ClownCarDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class ClownCarOpenTrunkDoAfterEvent : SimpleDoAfterEvent;

public sealed partial class ThankRiderActionEvent : InstantActionEvent;
public sealed partial class QuietBackThereActionEvent : InstantActionEvent;
public sealed partial class DrivingWithStyleActionEvent : InstantActionEvent;
