// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Goobstation.Shared.MantisBlades;
using Content.Medical.Common.Body;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Body;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Goobstation.Server.PairedExtendable.Systems;

// TODO: move this shit to shared and predict bruh
public sealed class MantisBladesSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly CommonBodyPartSystem _part = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PairedExtendableSystem _pairedExtendable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MantisBladeArmComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MantisBladeArmComponent, OrganEnabledEvent>(OnEnabled);
        SubscribeLocalEvent<MantisBladeArmComponent, ToggleMantisBladeEvent>(OnToggle);
        SubscribeLocalEvent<MantisBladeArmComponent, OrganDisabledEvent>(OnDisabled);
    }

    private void OnMapInit(Entity<MantisBladeArmComponent> ent, ref MapInitEvent args)
    {
        if (_body.GetBody(ent.Owner) is {} body)
            AddAction(ent, body);
    }

    private void OnEnabled(Entity<MantisBladeArmComponent> ent, ref OrganEnabledEvent args)
    {
        AddAction(ent, args.Body);
    }

    private void AddAction(Entity<MantisBladeArmComponent> ent, EntityUid body)
    {
        EnsureComp<ActionsContainerComponent>(ent);
        _actions.AddAction(body, ref ent.Comp.ActionUid, ent.Comp.ActionProto, ent);
    }

    private void OnToggle(Entity<MantisBladeArmComponent> ent, ref ToggleMantisBladeEvent args)
    {
        if (_body.GetBody(ent.Owner) is not {} body)
            return;

        if (!HasComp<EnabledOrganComponent>(ent))
        {
            _popup.PopupEntity(Loc.GetString("mantis-blade-disabled-emp"), ent, body);
            return;
        }

        var handLocation = _part.GetSymmetry(ent.Owner) switch
        {
            BodyPartSymmetry.Left => HandLocation.Left,
            BodyPartSymmetry.Right => HandLocation.Right,
            BodyPartSymmetry.None => HandLocation.Middle,
            _ => throw new ArgumentOutOfRangeException(),
        };

        args.Handled = _pairedExtendable.ToggleExtendable(body,
            ent.Comp.BladeProto,
            handLocation,
            out ent.Comp.BladeUid,
            ent.Comp.BladeUid);

        if (args.Handled)
            _audio.PlayPvs(ent.Comp.BladeUid == null ? ent.Comp.RetractSound : ent.Comp.ExtendSound, ent);
    }

    private void OnDisabled(Entity<MantisBladeArmComponent> ent, ref OrganDisabledEvent args)
    {
        Del(ent.Comp.BladeUid);
        Del(ent.Comp.ActionUid);
    }
}
