// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Trauma.Shared.Actions;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Network;

namespace Content.Trauma.Shared.Genetics.Abilities;

/// <summary>
/// Handles most things chemspike related.
/// </summary>
public sealed class ChemSpikeMutationSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedBloodstreamSystem _blood = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedProjectileSystem _projectile = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChemSpikeMutationComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<ChemTransferActionComponent, ChemTransferActionEvent>(OnTransfer);

        SubscribeLocalEvent<ChemTransferProjectileComponent, EmbedEvent>(OnProjectileEmbed);
        SubscribeLocalEvent<ChemTransferProjectileComponent, EmbedDetachEvent>(OnProjectileDetach);
        SubscribeLocalEvent<ChemTransferProjectileComponent, ComponentShutdown>(OnProjectileShutdown);
    }

    private void OnShutdown(Entity<ChemSpikeMutationComponent> ent, ref ComponentShutdown args)
    {
        PredictedQueueDel(ent.Comp.ActionEntity);
    }

    private void OnTransfer(Entity<ChemTransferActionComponent> ent, ref ChemTransferActionEvent args)
    {
        // client shouldn't predict this since the target might have left PVS range
        if (_net.IsClient ||
            args.Action.Comp.Container is not {} mutation ||
            !TryComp<ChemSpikeMutationComponent>(mutation, out var comp) ||
            comp.Target is not {} target)
            return;

        if (_blood.FlushChemicals(args.Performer, comp.MaxQuantity) is {} removed)
            _blood.TryAddToBloodstream(target, removed);

        args.Handled = true;

        // don't let it be used again, even if it didn't succeed.
        if (comp.Projectile is {} projectile)
            _projectile.EmbedDetach(projectile, null);
        SetMutationTarget((mutation, comp), null, null);
    }

    private void OnProjectileEmbed(Entity<ChemTransferProjectileComponent> ent, ref EmbedEvent args)
    {
        if (HasComp<BloodstreamComponent>(args.Embedded))
            SetProjectileTarget(ent, args.Embedded);
    }

    private void OnProjectileDetach(Entity<ChemTransferProjectileComponent> ent, ref EmbedDetachEvent args)
    {
        SetProjectileTarget(ent, null);
    }

    private void OnProjectileShutdown(Entity<ChemTransferProjectileComponent> ent, ref ComponentShutdown args)
    {
        SetProjectileTarget(ent, null);
    }

    private void SetProjectileTarget(EntityUid uid, EntityUid? target)
    {
        if (CompOrNull<ActionProjectileComponent>(uid)?.Container is not {} mutation)
            return;

        SetMutationTarget(mutation, target, uid);
    }

    private void SetMutationTarget(Entity<ChemSpikeMutationComponent?> ent, EntityUid? target, EntityUid? proj = null)
    {
        if (!Resolve(ent, ref ent.Comp) ||
            (ent.Comp.Target == target && ent.Comp.Projectile == proj) ||
            _mutation.GetMutationTarget(ent.Owner) is not {} user)
            return;

        ent.Comp.Target = target;
        ent.Comp.Projectile = proj;

        if (target != null)
            _actions.AddAction(user, ref ent.Comp.ActionEntity, ent.Comp.Action, container: ent.Owner);
        else
            _actions.RemoveAction(ent.Comp.ActionEntity);

        var key = target != null ? "set" : "reset";
        var msg = Loc.GetString("MutationChemSpike-target-" + key);
        if (target != null)
            _popup.PopupEntity(msg, user, user); // projectile embed isn't predicted FSR
        else
            _popup.PopupClient(msg, user, user);
    }
}
