// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Guardian;
using Content.Shared.Guardian.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;

namespace Content.Trauma.Shared.HoloParasite;

public abstract partial class HoloParasitePickerSystem : EntitySystem
{
    [Dependency] protected SharedPopupSystem Popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HoloParasitePickerComponent, UseInHandEvent>(OnInjectorHandled,
            before: [typeof(GuardianSystem)]);
        SubscribeLocalEvent<HoloParasitePickerComponent, AfterInteractEvent>(OnInjectorPoked,
            before: [typeof(GuardianSystem)]);
        SubscribeLocalEvent<HoloParasitePickerComponent, BoundUIOpenedEvent>(OnPanelOpened);
        Subs.BuiEvents<HoloParasitePickerComponent>(HoloParasitePickerUiKey.Key, subs =>
        {
            subs.Event<HoloParasitePickMessage>(OnPickMessage);
        });
    }

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

    private void OnInjectorPoked(Entity<HoloParasitePickerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || ent.Comp.Variants.Count == 0 || args.Target == null || !args.CanReach)
            return;

        args.Handled = true;

        if (TryComp<GuardianCreatorComponent>(ent, out var creator) && creator.Used)
        {
            Popup.PopupEntity(Loc.GetString("holoparasite-picker-already-used"), ent, args.User);
            return;
        }

        OpenPicker(ent, args.User, args.Target.Value);
    }

    protected virtual void OpenPicker(Entity<HoloParasitePickerComponent> ent, EntityUid user, EntityUid host)
    {
    }

    private void OnPanelOpened(Entity<HoloParasitePickerComponent> ent, ref BoundUIOpenedEvent args)
    {
        RefreshPanel(ent);
    }

    protected virtual void RefreshPanel(Entity<HoloParasitePickerComponent> ent)
    {
    }

    private void OnPickMessage(Entity<HoloParasitePickerComponent> ent, ref HoloParasitePickMessage args)
    {
        ApplyChoice(ent, args.Actor, args.ChosenProto);
    }

    protected virtual void ApplyChoice(Entity<HoloParasitePickerComponent> ent, EntityUid user, string protoId)
    {
    }
}
