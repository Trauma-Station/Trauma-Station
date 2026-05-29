// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Abductor;
using Content.Medical.Shared.Surgery;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Medical.Shared.Abductor;

public abstract partial class SharedAbductorSystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] protected SharedColorFlashEffectSystem _color = default!;
    [Dependency] protected SharedDoAfterSystem _doAfter = default!;
    [Dependency] protected SharedPopupSystem _popup = default!;
    [Dependency] protected TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> Abductor = "Abductor";

    private void InitializeGizmo()
    {
        SubscribeLocalEvent<AbductorGizmoComponent, AfterInteractEvent>(OnGizmoInteract);
        SubscribeLocalEvent<AbductorGizmoComponent, MeleeHitEvent>(OnGizmoHitInteract);

        SubscribeLocalEvent<AbductorGizmoComponent, AbductorGizmoMarkDoAfterEvent>(OnGizmoDoAfter);
        SubscribeLocalEvent<AbductorGizmoComponent, ActivateInWorldEvent>(OnGizmoToggleMode);
    }

    private void OnGizmoHitInteract(Entity<AbductorGizmoComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count != 1)
            return;
        var target = args.HitEntities[0];
        if (!HasComp<SurgeryTargetComponent>(target))
            return;
        if (ent.Comp.BrainwashMode)
        {
            GizmoBrainWashUse(ent, target, args.User);
            return;
        }
        GizmoUse(ent, target, args.User);
    }

    private void OnGizmoInteract(Entity<AbductorGizmoComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not {} target)
            return;

        if (HasComp<SurgeryTargetComponent>(target))
        {
            args.Handled = true;
            if (ent.Comp.BrainwashMode)
            {
                GizmoBrainWashUse(ent, target, args.User);
                return;
            }
            GizmoUse(ent, target, args.User);
            return;
        }

        if (!TryComp<AbductorConsoleComponent>(target, out var console))
            return;

        args.Handled = true;

        console.Target = ent.Comp.Target;
        Dirty(target, console);
        _popup.PopupClient(Loc.GetString("abductors-ui-gizmo-transferred"), ent, args.User);
        var flashed = new List<EntityUid>(2) { ent.Owner, target };
        var filter = Filter.Local();
        var user = args.User;
        if (_net.IsServer) // evil
            filter = Filter.Pvs(args.User, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == user);
        _color.RaiseEffect(Color.FromHex("#00BA00"), flashed, filter);
        UpdateGui(console.Target, (target, console));
    }

    private void GizmoUse(Entity<AbductorGizmoComponent> ent, EntityUid target, EntityUid user)
    {
        if (HasComp<AbductorComponent>(target))
            return;

        var time = TimeSpan.FromSeconds(6);
        if (_tag.HasTag(target, Abductor))
            time = TimeSpan.FromSeconds(0.5);

        var doAfter = new DoAfterArgs(EntityManager, user, time, new AbductorGizmoMarkDoAfterEvent(), ent, target, ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            DistanceThreshold = 1f
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void GizmoBrainWashUse(Entity<AbductorGizmoComponent> ent, EntityUid target, EntityUid user)
    {
        if (HasComp<AbductorComponent>(target))
            return;
        var doAfterArgs = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(10), new BrainwashDoAfterEvent(), ent, target, ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            DistanceThreshold = 1f
        };
        _doAfter.TryStartDoAfter(doAfterArgs);

    }

    private void OnGizmoDoAfter(Entity<AbductorGizmoComponent> ent, ref AbductorGizmoMarkDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not {} target)
            return;

        ent.Comp.Target = GetNetEntity(target);
        EnsureComp<AbductorVictimComponent>(target, out var victimComponent);
        victimComponent.LastActivation = Timing.CurTime + TimeSpan.FromMinutes(5);
        victimComponent.Position ??= EnsureComp<TransformComponent>(args.Target.Value).Coordinates;

        args.Handled = true;
    }

    private void OnGizmoToggleMode(Entity<AbductorGizmoComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;
        ent.Comp.BrainwashMode = !ent.Comp.BrainwashMode;
        Dirty(ent);
        var modeName = Loc.GetString(ent.Comp.BrainwashMode ? "abductors-gizmo-mode-brainwash" : "abductors-gizmo-mode-mark");
        _popup.PopupClient(Loc.GetString("abductors-gizmo-mode-changed", ("mode", modeName)), ent, args.User);
    }
}
