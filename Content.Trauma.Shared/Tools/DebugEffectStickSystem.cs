// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Administration.Managers;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Trauma.Shared.EntityEffects;

namespace Content.Trauma.Shared.Tools;

public sealed class DebugEffectStickSystem : EntitySystem
{
    [Dependency] private readonly EffectDataSystem _data = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ISharedAdminManager _admin = default!;
    [Dependency] private readonly NestedEffectSystem _nested = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DebugEffectStickComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<DebugEffectStickComponent, AfterInteractEvent>(OnAfterInteract);

        Subs.BuiEvents<DebugEffectStickComponent>(DebugEffectStickUiKey.Key, subs =>
        {
            subs.Event<DebugStickSetEffectMessage>(OnSetEffect);
        });
    }

    private void OnUseInHand(Entity<DebugEffectStickComponent> ent, ref UseInHandEvent args)
    {
        var user = args.User;
        // sorry jimbo admins only
        if (args.Handled || !IsWorthy(user))
            return;

        args.Handled = _ui.TryOpenUi(ent.Owner, DebugEffectStickUiKey.Key, user, predicted: true);
    }

    private void OnSetEffect(Entity<DebugEffectStickComponent> ent, ref DebugStickSetEffectMessage args)
    {
        var user = args.Actor;
        if (ent.Comp.Effect == args.Effect || !IsWorthy(user))
            return;

        _adminLogger.Add(LogType.AdminCommands, LogImpact.High, $"{ToPrettyString(user)} changed DEBUG EFFECT STICK {ToPrettyString(ent)} to {args.Effect}");
        ent.Comp.Effect = args.Effect;
        Dirty(ent);
    }

    private void OnAfterInteract(Entity<DebugEffectStickComponent> ent, ref AfterInteractEvent args)
    {
        var user = args.User;
        if (args.Target is not {} target || ent.Comp.Effect is not {} effect)
            return;

        args.Handled = true;

        // you have to explicitly VV it and allow plebians to use it
        // be very fucking sure it's safe if you do this
        // setting the effect can never be allowed by non-admins
        if (ent.Comp.Unsafe && !IsWorthy(user))
            return;

        _adminLogger.Add(LogType.AdminCommands, LogImpact.High, $"{ToPrettyString(user)} used DEBUG EFFECT STICK {ToPrettyString(ent)} on {ToPrettyString(target)} with effect {effect}");

        _data.SetTool(target, ent);
        _nested.ApplyNestedEffect(target, effect, user: user);
        _data.ClearTool(target);
    }

    public bool IsWorthy(EntityUid uid)
    {
        // equivalent to a very good VV
        if (_admin.HasAdminFlag(uid, AdminFlags.VarEdit))
            return true;

        _popup.PopupClient("You are not worthy...", uid, uid);
        return false;
    }
}
