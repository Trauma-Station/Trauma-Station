// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Server.Xenomorphs.Plasma;
using Content.Server.Actions;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Trauma.Shared.Actions;
using Content.Trauma.Shared.Xenomorphs;
using Content.Trauma.Shared.Xenomorphs.Caste;
using Content.Trauma.Shared.Xenomorphs.Queen;
using Content.Trauma.Shared.Xenomorphs.Xenomorph;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;

namespace Content.Trauma.Server.Xenomorphs.Queen;

public sealed partial class XenomorphQueenSystem : EntitySystem
{
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private PlasmaSystem _plasma = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    private static readonly ProtoId<XenomorphCastePrototype> PraetorianCaste = "Praetorian";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenomorphQueenComponent, PromotionActionEvent>(OnPromotionAction);
        SubscribeLocalEvent<XenomorphQueenComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<XenomorphQueenComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(EntityUid uid, XenomorphQueenComponent component, MapInitEvent args) =>
        _actions.AddAction(uid, ref component.PromotionAction, component.PromotionActionId);

    private void OnShutdown(EntityUid uid, XenomorphQueenComponent component, ComponentShutdown args) =>
        _actions.RemoveAction(uid, component.PromotionAction);

    private void OnPromotionAction(EntityUid uid, XenomorphQueenComponent component, PromotionActionEvent args)
    {
        // Goobstation start
        var user = args.Performer;
        if (args.Target == EntityUid.Invalid || args.Target == user)
            return;

        // Additional validation in case the target is no longer valid
        if (!HasComp<XenomorphComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("xenomorphs-queen-promotion-invalid-target"), user, user);
            return;
        }

        if (!TryComp<XenomorphComponent>(args.Target, out var xenomorph))
            return;

        // Check if target is already a Praetorian or not in the whitelist
        if (xenomorph.Caste == PraetorianCaste || !component.CasteWhitelist.Contains(xenomorph.Caste))
        {
            if (xenomorph.Caste == PraetorianCaste)
                _popup.PopupEntity(Loc.GetString("xenomorphs-queen-already-praetorian"), user, user);
            else
                _popup.PopupEntity(Loc.GetString("xenomorphs-queen-promotion-didnt-pass-whitelist"), user, user);
            return;
        }

        // Try direct evolution with optional mind transfer
        var target = args.Target;
        var coordinates = Transform(target).Coordinates;
        var newXeno = Spawn(component.PromoteTo, coordinates);

        // Transfer mind if it exists
        if (_mind.TryGetMind(target, out var mindId, out var mind))
            _mind.TransferTo(mindId, newXeno, mind: mind);

        // Copy over any important components
        if (TryComp<XenomorphComponent>(newXeno, out var newXenoComp) &&
            TryComp<XenomorphComponent>(target, out var oldXenoComp))
        {
            newXenoComp.Caste = oldXenoComp.Caste;
        }

        // Update the caste to Praetorian for the new entity
        if (TryComp<XenomorphComponent>(newXeno, out var xenomorphComp))
        {
            xenomorphComp.Caste = PraetorianCaste;
            Dirty(newXeno, xenomorphComp);
        }

        // Get the target's name before deleting the entity
        var targetName = Name(target);

        // Clean up the old entity
        Del(target);

        // Deduct plasma cost if applicable
        _plasma.ChangePlasmaAmount(uid, -500f); // Deduct 500 plasma for the promotion
        _popup.PopupEntity(
            Loc.GetString("xenomorphs-queen-promotion-success", ("target", targetName)), uid, uid);

        args.Handled = true;
        // Goobstation end
    }

    public bool IsQueenAlive(EntityUid? exclude = null)
    {
        var query = EntityQueryEnumerator<XenomorphQueenComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (uid == exclude)
                continue;

            if (!_mobState.IsDead(uid))
                return true;
        }
        return false;
    }
}
