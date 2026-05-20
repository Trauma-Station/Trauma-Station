using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Whitelist;

namespace Content.Shared.Pinpointer;

public abstract partial class SharedPinpointerSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] protected EntityWhitelistSystem Whitelist = default!; // Goob edit
    [Dependency] private SharedPopupSystem _popup = default!; // Goob edit

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PinpointerComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<PinpointerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PinpointerComponent, ExaminedEvent>(OnExamined);
    }

    /// <summary>
    ///     Set the target if capable
    /// </summary>
    private void OnAfterInteract(Entity<PinpointerComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not { } target || args.Handled)
            return;

        if (!ent.Comp.CanRetarget || ent.Comp.IsActive)
            return;

        // Goob edit start: retargeting has a whitelist
        args.Handled = true;

        if (Whitelist.IsWhitelistFail(ent.Comp.RetargetingWhitelist, target) ||
            Whitelist.IsWhitelistPass(ent.Comp.RetargetingBlacklist, target))
        {
            return;
        }

        // TODO add doafter once the freeze is lifted
        // ignore can target multiple, because too hard to support
        ent.Comp.Targets.Clear();
        ent.Comp.Targets.Add(target);
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):player} set target of {ToPrettyString(ent):pinpointer} to {ToPrettyString(target):target}");
        if (ent.Comp.UpdateTargetName)
            ent.Comp.TargetName = Identity.Name(target, EntityManager);

        _popup.PopupPredicted(Loc.GetString("pinpointer-link-success"), ent, args.User);
        // Goob edit end
    }

    /// <summary>
    ///     Set pinpointers target to track
    ///     Goob edit: If CanTargetMultiple is true in Pinpointer component, then it will be ADDED, not set
    /// </summary>
    public virtual void SetTarget(Entity<PinpointerComponent?> ent, EntityUid? target)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (target == null || ent.Comp.Targets.Contains(target.Value))
        {
            return;
        }

        if (!ent.Comp.CanTargetMultiple)
        {
            ent.Comp.Targets.Clear();
        }

        if (TerminatingOrDeleted(target.Value))
        {
            TrySetArrowAngle(ent, Angle.Zero);
            return;
        }

        ent.Comp.Targets.Add(target.Value);

        if (ent.Comp.UpdateTargetName)
            ent.Comp.TargetName = Identity.Name(target.Value, EntityManager);
        // WD EDIT START - UpdateDirectionToTarget is triggered when updating, no need to run it again
        // if (ent.Comp.IsActive)
        //    UpdateDirectionToTarget(uid, ent.Comp);
        // WD EDIT END
    }

    /// <summary>
    /// Goob edit: sets a list of targets for a pinpointer.
    /// </summary>
    public virtual void SetTargets(Entity<PinpointerComponent?> ent, List<EntityUid> targets)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!ent.Comp.CanTargetMultiple)
            return; // No.

        var targetsList = targets.Where(Exists).ToList();

        ent.Comp.Targets = targetsList;

        /* Trauma - UpdateDirectionToTarget is triggered when updating, no need to run it again
        if (ent.Comp.IsActive)
            UpdateDirectionToTarget(ent);
        */
    }

    /// <summary>
    ///     Update direction from pinpointer to selected target (if it was set)
    /// </summary>
    protected virtual void UpdateDirectionToTarget(Entity<PinpointerComponent?> ent)
    {

    }

    private void OnExamined(Entity<PinpointerComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.CanExamine || !args.IsInDetailsRange || ent.Comp.TargetName == null) // Trauma - check CanExamine
            return;

        args.PushMarkup(Loc.GetString("examine-pinpointer-linked", ("target", ent.Comp.TargetName)));
    }

    /// <summary>
    ///     Manually set distance from pinpointer to target
    /// </summary>
    public void SetDistance(Entity<PinpointerComponent?> ent, Distance distance)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (distance == ent.Comp.DistanceToTarget)
            return;

        ent.Comp.DistanceToTarget = distance;
        Dirty(ent);
    }

    /// <summary>
    ///     Try to manually set pinpointer arrow direction.
    ///     If difference between current angle and new angle is smaller than
    ///     pinpointer precision, new value will be ignored and it will return false.
    /// </summary>
    public bool TrySetArrowAngle(Entity<PinpointerComponent?> ent, Angle arrowAngle)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Comp.ArrowAngle.EqualsApprox(arrowAngle, ent.Comp.Precision))
            return false;

        ent.Comp.ArrowAngle = arrowAngle;
        Dirty(ent);

        return true;
    }

    /// <summary>
    ///     Activate/deactivate pinpointer screen. If it has target it will start tracking it.
    /// </summary>
    public void SetActive(Entity<PinpointerComponent?> ent, bool isActive)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (isActive == ent.Comp.IsActive)
            return;

        ent.Comp.IsActive = isActive;
        Dirty(ent);
    }


    /// <summary>
    ///     Toggle Pinpointer screen. If it has target it will start tracking it.
    /// </summary>
    /// <returns>True if pinpointer was activated, false otherwise</returns>
    public virtual bool TogglePinpointer(Entity<PinpointerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var isActive = !ent.Comp.IsActive;
        SetActive(ent, isActive);
        return isActive;
    }

    private void OnEmagged(Entity<PinpointerComponent> ent, ref GotEmaggedEvent args)
    {
        // <Trauma>
        if (!ent.Comp.CanEmag)
            return;
        // </Trauma>

        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        // <Trauma> - fully changed the logic
        args.Handled = true;

        if (ent.Comp.CanRetarget)
            ent.Comp.RetargetingWhitelist = null; // Can target anything
        else
            ent.Comp.CanRetarget = true;
        // </Trauma>
    }
}
