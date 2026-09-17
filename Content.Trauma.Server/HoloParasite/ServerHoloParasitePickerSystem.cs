// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.Guardian;
using Content.Shared.Guardian.Components;
using Content.Shared.IdentityManagement;
using Content.Trauma.Shared.HoloParasite;
using Robust.Server.GameObjects;

namespace Content.Trauma.Server.HoloParasite;

public sealed partial class ServerHoloParasitePickerSystem : HoloParasitePickerSystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    [Dependency] private EntityQuery<GuardianHostComponent> _hostQuery = default!;

    protected override void OpenPicker(Entity<HoloParasitePickerComponent> ent, EntityUid user, EntityUid host)
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

        _ui.TryOpenUi(ent.Owner, HoloParasitePickerUiKey.Key, user);
    }

    protected override void RefreshPanel(Entity<HoloParasitePickerComponent> ent)
    {
        var choices = new List<HoloParasiteChoice>();
        var startIndex = 0;

        for (var i = 0; i < ent.Comp.Variants.Count; i++)
        {
            var variant = ent.Comp.Variants[i];
            var committed = ent.Comp.ChosenVariant == variant.Prototype.Id;
            if (committed)
                startIndex = i;

            choices.Add(new HoloParasiteChoice(
                Loc.GetString(variant.Caption),
                variant.Synopsis == null ? null : Loc.GetString(variant.Synopsis),
                variant.Lore == null ? null : Loc.GetString(variant.Lore),
                variant.Prototype.Id));
        }

        _ui.SetUiState(ent.Owner, HoloParasitePickerUiKey.Key, new HoloParasitePickerState(choices, startIndex));
    }

    protected override void ApplyChoice(
        Entity<HoloParasitePickerComponent> ent,
        EntityUid user,
        string protoId)
    {
        if (!TryComp<GuardianCreatorComponent>(ent, out var creator))
            return;

        if (creator.Used)
        {
            Popup.PopupEntity(Loc.GetString("holoparasite-picker-already-used"), ent, user);
            _ui.CloseUi(ent.Owner, HoloParasitePickerUiKey.Key, user);
            return;
        }

        if (ent.Comp.Variants.All(variant => variant.Prototype.Id != protoId))
            return;

        if (ent.Comp.DetectedHosts.Count == 0)
            return;

        var host = ent.Comp.DetectedHosts[0];
        if (TerminatingOrDeleted(host) || _hostQuery.HasComp(host))
            return;

        creator.GuardianProto = protoId;
        ent.Comp.ChosenVariant = protoId;

        _ui.CloseUi(ent.Owner, HoloParasitePickerUiKey.Key, user);

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
