// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.Guardian;
using Content.Shared.Guardian.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;

namespace Content.Trauma.Shared.HoloParasite;

public abstract partial class HoloParasitePickerSystem : EntitySystem
{
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    [Dependency] private EntityQuery<GuardianHostComponent> _hostQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<HoloParasitePickerComponent>(HoloParasitePickerUiKey.Key, subs =>
        {
            subs.Event<HoloParasitePickMessage>(OnPickMessage);
        });
    }

    [SubscribeLocalEvent(before: [typeof(GuardianSystem)])]
    private void OnInjectorHandled(Entity<HoloParasitePickerComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || ent.Comp.Variants.Count == 0)
            return;

        args.Handled = true;

        if (TryComp<GuardianCreatorComponent>(ent, out var creator) && creator.Used)
        {
            Popup.PopupEntity(Loc.GetString("holoparasite-picker-already-used"), ent, args.User);
            return;
        }

        OpenPicker(ent, args.User, args.User);
    }

    [SubscribeLocalEvent(before: [typeof(GuardianSystem)])]
    private void OnInjectorPoked(Entity<HoloParasitePickerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || ent.Comp.Variants.Count == 0 || args.Target is not { } target || !args.CanReach)
            return;

        args.Handled = true;

        if (TryComp<GuardianCreatorComponent>(ent, out var creator) && creator.Used)
        {
            Popup.PopupEntity(Loc.GetString("holoparasite-picker-already-used"), ent, args.User);
            return;
        }

        OpenPicker(ent, args.User, target);
    }

    private void OpenPicker(Entity<HoloParasitePickerComponent> ent, EntityUid user, EntityUid host)
    {
        if (!HasComp<CanHostGuardianComponent>(host))
        {
            var msg = Loc.GetString("holoparasite-picker-invalid-target",
                ("entity", Identity.Entity(host, EntityManager, user)));
            Popup.PopupEntity(msg, user, user);
            return;
        }

        if (_hostQuery.HasComp(host))
        {
            Popup.PopupEntity(Loc.GetString("holoparasite-picker-host-occupied"), ent, user);
            return;
        }

        ent.Comp.DetectedHosts.Clear();
        ent.Comp.DetectedHosts.Add(host);

        if (ent.Comp.ChosenVariant == null)
            ent.Comp.ChosenVariant = ent.Comp.Variants[0].Prototype;

        _ui.TryOpenUi(ent.Owner, HoloParasitePickerUiKey.Key, user, predicted: true);
    }

    private void OnPickMessage(Entity<HoloParasitePickerComponent> ent, ref HoloParasitePickMessage args)
    {
        ApplyChoice(ent, args.Actor, args.ChosenProto);
    }

    private void ApplyChoice(Entity<HoloParasitePickerComponent> ent, EntityUid user, string protoId)
    {
        if (!TryComp<GuardianCreatorComponent>(ent, out var creator))
            return;

        if (creator.Used)
        {
            Popup.PopupEntity(Loc.GetString("holoparasite-picker-already-used"), ent, user);
            _ui.CloseUi(ent.Owner, HoloParasitePickerUiKey.Key, user, predicted: true);
            return;
        }

        if (!ent.Comp.Variants.Select(variant => variant.Prototype.Id).Contains(protoId))
            return;

        if (ent.Comp.DetectedHosts.Count == 0)
            return;

        var host = ent.Comp.DetectedHosts[0];
        if (TerminatingOrDeleted(host) || _hostQuery.HasComp(host))
            return;

        creator.GuardianProto = protoId;
        ent.Comp.ChosenVariant = protoId;

        _ui.CloseUi(ent.Owner, HoloParasitePickerUiKey.Key, user, predicted: true);

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            creator.InjectionDelay,
            new GuardianCreatorDoAfterEvent(),
            ent,
            target: host,
            used: ent)
        {
            BreakOnMove = true,
            NeedHand = true,
            BreakOnHandChange = true
        });
    }
}
