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
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Medical.Shared.Abductor;

public abstract partial class SharedAbductorSystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] protected readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> Abductor = "Abductor";

    private void InitializeGizmo()
    {
        SubscribeLocalEvent<AbductorGizmoComponent, AfterInteractEvent>(OnGizmoInteract);
        SubscribeLocalEvent<AbductorGizmoComponent, MeleeHitEvent>(OnGizmoHitInteract);

        SubscribeLocalEvent<AbductorGizmoComponent, AbductorGizmoMarkDoAfterEvent>(OnGizmoDoAfter);
    }

    private void OnGizmoHitInteract(Entity<AbductorGizmoComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count != 1) return;
        var target = args.HitEntities[0];
        if (!HasComp<SurgeryTargetComponent>(target)) return;
        GizmoUse(ent, target, args.User);
    }

    private void OnGizmoInteract(Entity<AbductorGizmoComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not {} target)
            return;

        if (HasComp<SurgeryTargetComponent>(target))
        {
            args.Handled = true;
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
}
