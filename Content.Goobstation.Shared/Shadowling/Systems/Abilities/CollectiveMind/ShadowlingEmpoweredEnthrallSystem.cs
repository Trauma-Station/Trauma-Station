// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mindshield; // Trauma - no bypass mindshields
using Content.Goobstation.Shared.Shadowling.Components.Abilities.CollectiveMind;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Popups;

namespace Content.Goobstation.Shared.Shadowling.Systems.Abilities.CollectiveMind;

/// <summary>
/// This handles Empowered Enthrall.
/// Empowered enthrall is like the basic enthrall, however it can bypass Mindshields, has slightly longer range.
/// It also takes less time to enthrall someone.
/// </summary>
public sealed partial class ShadowlingEmpoweredEnthrallSystem : EntitySystem
{
    [Dependency] private MindShieldSystem _mindShield = default!; // Trauma - no bypass mindshields
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedShadowlingSystem _shadowling = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedActionsSystem _actions = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowlingEmpoweredEnthrallComponent, EmpoweredEnthrallEvent>(OnEmpEnthrall);
        SubscribeLocalEvent<ShadowlingEmpoweredEnthrallComponent, EmpoweredEnthrallDoAfterEvent>(OnEmpEnthrallDoAfter);
        SubscribeLocalEvent<ShadowlingEmpoweredEnthrallComponent, MapInitEvent>(OnStartup);
        SubscribeLocalEvent<ShadowlingEmpoweredEnthrallComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<ShadowlingEmpoweredEnthrallComponent> ent, ref MapInitEvent args)
        => _actions.AddAction(ent.Owner, ref ent.Comp.ActionEnt, ent.Comp.ActionId);

    private void OnShutdown(Entity<ShadowlingEmpoweredEnthrallComponent> ent, ref ComponentShutdown args)
        => _actions.RemoveAction(ent.Owner, ent.Comp.ActionEnt);

    private void OnEmpEnthrall(EntityUid uid, ShadowlingEmpoweredEnthrallComponent component, EmpoweredEnthrallEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;
        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            uid,
            component.EnthrallTime,
            new EmpoweredEnthrallDoAfterEvent(),
            uid,
            target)
        {
            CancelDuplicate = true,
            BreakOnDamage = true,
        };

        if (!_shadowling.CanEnthrall(uid, target))
            return;
        // <Trauma> - no bypass mindshields
        if (_mindShield.IsShielded(target))
        {
            _popup.PopupEntity(Loc.GetString("shadowling-enthrall-mindshield"), uid, uid, PopupType.SmallCaution);
            return;
        }
        // </Trauma>
        _popup.PopupEntity(Loc.GetString("shadowling-target-being-thralled"), target, target, PopupType.SmallCaution);

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnEmpEnthrallDoAfter(EntityUid uid, ShadowlingEmpoweredEnthrallComponent component, EmpoweredEnthrallDoAfterEvent args)
    {
        if (args.Handled
            || args.Cancelled)
            return;

        _shadowling.DoEnthrall(uid, component.EnthrallComponents, args);
        args.Handled = true;
    }
}
