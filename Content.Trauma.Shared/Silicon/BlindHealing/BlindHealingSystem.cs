// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Silicon.BlindHealing;

public sealed partial class BlindHealingSystem : EntitySystem
{
    [Dependency] private BlindableSystem _blindable = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStackSystem _stack = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlindHealingComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<BlindHealingComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<BlindHealingComponent, HealingDoAfterEvent>(OnHealingFinished);
    }

    private void OnHealingFinished(Entity<BlindHealingComponent> ent, ref HealingDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not { } target ||
            !TryComp<BlindableComponent>(target, out var blindable) ||
            blindable.EyeDamage == 0)
            return;

        if (TryComp<StackComponent>(ent, out var stack))
            _stack.TryUse((ent.Owner, stack), 1);

        _blindable.AdjustEyeDamage((target, blindable), -blindable.EyeDamage);

        _adminLog.Add(LogType.Healed, $"{args.User:user} repaired {target:target}'s vision");

        var str = Loc.GetString("comp-repairable-repair",
            ("target", target),
            ("tool", ent));
        _popup.PopupClient(str, ent, args.User);
    }

    private bool TryHealBlindness(Entity<BlindHealingComponent> ent, EntityUid user, EntityUid target)
    {
        if (!_whitelist.IsWhitelistPass(ent.Comp.Whitelist, target) ||
            !TryComp<BlindableComponent>(target, out var blindable) ||
            blindable.EyeDamage == 0 ||
            user == target && !ent.Comp.AllowSelfHeal)
            return false;

        var delay = ent.Comp.DoAfterDelay;
        if (user == target)
            delay *= ent.Comp.SelfHealPenalty;
        var args = new DoAfterArgs(
            EntityManager,
            user,
            delay,
            new HealingDoAfterEvent(),
            ent,
            target: target,
            used: ent)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
        };

        return _doAfter.TryStartDoAfter(args);
    }

    private void OnInteract(Entity<BlindHealingComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target || !args.CanReach)
            return;

        args.Handled = TryHealBlindness(ent, args.User, target);
    }

    private void OnUse(Entity<BlindHealingComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryHealBlindness(ent, args.User, target: args.User);
    }
}

[Serializable, NetSerializable]
public sealed partial class HealingDoAfterEvent : SimpleDoAfterEvent;
