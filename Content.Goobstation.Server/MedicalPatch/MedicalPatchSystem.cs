// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Sticky;
using Content.Shared.Sticky.Components;
using Content.Shared.Sticky.Systems;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server.MedicalPatch;

public sealed partial class MedicalPatchSystem : EntitySystem
{
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private StickySystem _sticky = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private ReactiveSystem _reactive = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedicalPatchComponent, EntityUnstuckEvent>(OnUnstuck);
        SubscribeLocalEvent<MedicalPatchComponent, EntityStuckEvent>(OnStuck);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // TODO: make active component for this bruh
        var query = EntityQueryEnumerator<MedicalPatchComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.NextUpdate)
                continue;

            if (!TryComp<StickyComponent>(uid, out var stickycomp))
                continue;
            if (stickycomp.StuckTo == null)
                continue;

            comp.NextUpdate = _timing.CurTime + TimeSpan.FromSeconds(comp.UpdateTime);

            Cycle(uid, comp);
        }
    }
    public void Cycle(EntityUid uid, MedicalPatchComponent component)
    {
        if (!TryInject(uid, component, component.TransferAmount))
        {
            if (!TryComp<StickyComponent>(uid, out var stickycomp))
                return;
            _sticky.UnstickFromEntity((uid, stickycomp), uid);
        }
    }
    public bool TryInject(EntityUid uid, MedicalPatchComponent component, FixedPoint2 transferAmount)
    {
        if (!TryComp<StickyComponent>(uid, out var stickycomp))
            return false;

        if (stickycomp.StuckTo == null)
            return false;
        var target = (EntityUid) stickycomp.StuckTo;

        if (!_solution.TryGetSolution(uid, component.SolutionName, out var medicalPatchSoln, out var medicalPatchSolution) || medicalPatchSolution.Volume == 0)
        {
            //Solution Empty
            return false;
        }
        if (!_solution.TryGetInjectableSolution(target, out var targetSoln, out var targetSolution))
        {
            //_popup.PopupEntity(Loc.GetString("Medical Patch cant find a bloodsystem"), target);
            return false;
        }
        var realTransferAmount = FixedPoint2.Min(transferAmount, targetSolution.AvailableVolume);
        if (realTransferAmount <= 0)
        {
            _popup.PopupEntity(Loc.GetString("No room to inject"), target);
            return true;
        }
        var removedSolution = _solution.SplitSolution(medicalPatchSoln.Value, realTransferAmount);
        if (!targetSolution.CanAddSolution(removedSolution))
            return true;
        _reactive.DoEntityReaction(target, removedSolution, ReactionMethod.Injection);
        _solution.TryAddSolution(targetSoln.Value, removedSolution);
        return true;
    }
    public void OnStuck(EntityUid uid, MedicalPatchComponent component, ref EntityStuckEvent args)
    {
        if (!_solution.TryGetSolution(uid, component.SolutionName, out var medicalPatchSoln, out var medicalPatchSolution))
            return;

        //Logg the Patch stick to.
        _adminLogger.Add(LogType.ForceFeed, $"{ToPrettyString(args.User):user} stuck a patch on  {ToPrettyString(args.Target):target} using {ToPrettyString(uid):using} containing {SharedSolutionContainerSystem.ToPrettyString(medicalPatchSolution):medicalPatchSolution}");

        if (component.InjectAmmountOnAttatch > 0)
        {
            if (!TryInject(uid, component, component.InjectAmmountOnAttatch))
                return;
        }
        if (component.InjectPercentageOnAttatch > 0)
        {
            if (medicalPatchSolution.Volume == 0)
                return;
            if (!TryInject(uid, component, medicalPatchSolution.Volume * (component.InjectPercentageOnAttatch / 100)))
                return;
        }
    }
    public void OnUnstuck(EntityUid uid, MedicalPatchComponent component, ref EntityUnstuckEvent args)
    {
        if (component.SingleUse)
        {
            if (component.TrashObject!=null)
            {
                var coordinates = Transform(uid).Coordinates;
                var finisher = Spawn(component.TrashObject, coordinates);
                // If the user is holding the item
                if (_hands.IsHolding(args.User, uid, out var hand))
                {
                    Del(uid);

                    // Put the Medicalpatch in the user's hand
                    _hands.TryPickup(args.User, finisher, hand);
                    return;
                }
            }
            QueueDel(uid);
        }
    }
}
