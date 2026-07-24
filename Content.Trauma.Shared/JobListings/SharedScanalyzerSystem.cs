// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Objectives;
using Content.Shared.Objectives.Components;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;

namespace Content.Trauma.Shared.JobListings;

/// <summary>
/// System facilitating scanning grand theft items to complete progtot objectives.
/// The list of scanned grand theft items is stored on the traitor's mind via the <see cref="ScanalyzerMindArchiveComponent"/>
/// </summary>
public abstract partial class SharedScanalyzerSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ScanalyzerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<StealTargetComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ScanalyzerComponent, ScanalyzerScanDoAfterEvent>(OnScan);
        SubscribeLocalEvent<ScanalyzerRequiresPowerComponent, AttemptScanalyzerScanEvent>(OnAttemptScanWhenPowered);
    }

    /// <summary>
    /// Starts the scanning do-after. Does not check if the scan should happen, use <see cref="CanScan"/> before calling this.
    /// </summary>
    public void StartScan(Entity<ScanalyzerComponent> entity, EntityUid user, EntityUid target)
    {
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, entity.Comp.ScanDuration, new ScanalyzerScanDoAfterEvent(), entity.Owner, target, entity.Owner)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
            BreakOnWeightlessMove = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        });
    }

    /// <summary>
    /// Check if the scanalyzer can scan the target.
    /// </summary>
    public bool CanScan(Entity<ScanalyzerComponent> entity, Entity<StealTargetComponent> target)
    {
        if (entity.Comp.Used)
            return false;
        if (entity.Comp.StealTarget != target.Comp.StealGroup)
            return false;
        return true;
    }

    protected virtual void AfterScan(Entity<ScanalyzerComponent> entity, EntityUid user, ProtoId<StealTargetGroupPrototype> target)
    {

    }

    private void OnInteractUsing(Entity<StealTargetComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;
        if (!TryComp<ScanalyzerComponent>(args.Used, out var scanalyzer))
            return;
        if (!CanScan((args.Used, scanalyzer), entity))
            return;

        var ev = new AttemptScanalyzerScanEvent(entity.Owner, args.User);
        RaiseLocalEvent(args.Used, ref ev);
        if (ev.Cancelled)
            return;

        StartScan((args.Used, scanalyzer), args.User, entity.Owner);
        args.Handled = true;
    }

    private void OnScan(Entity<ScanalyzerComponent> entity, ref ScanalyzerScanDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is null)
            return;
        if (!TryComp<StealTargetComponent>(args.Target, out var steal))
            return;
        if (!CanScan(entity, (args.Target.Value, steal)))
            return;

        entity.Comp.Used = true;
        Dirty(entity);
        _popup.PopupClient(Loc.GetString("scanalyzer-popup-used"), args.User, PopupType.Medium);

        AfterScan(entity, args.User, steal.StealGroup);
        var ev = new ScanalyzerScanFinishedEvent(args.Target.Value, args.User);
        RaiseLocalEvent(entity, ref ev);
        args.Handled = true;
    }

    private void OnExamined(Entity<ScanalyzerComponent> entity, ref ExaminedEvent args)
    {
        if (!_proto.Resolve(entity.Comp.StealTarget, out var target))
            return;
        args.PushMarkup(Loc.GetString("scanalyzer-examine-steal-target", ("target", Loc.GetString(target.Name))));
        args.PushMarkup(entity.Comp.Used
            ? Loc.GetString("scanalyzer-examine-used")
            : Loc.GetString("scanalyzer-examine-not-used"));
    }

    private void OnAttemptScanWhenPowered(Entity<ScanalyzerRequiresPowerComponent> entity, ref AttemptScanalyzerScanEvent args)
    {
        if (!_power.IsPowered(args.Target))
            args.Cancelled = true;
    }
}

/// <summary>
/// Do after even for scanning an item.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ScanalyzerScanDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// Raised on the scanalyzer entity before it tries to do a scan.
/// </summary>
[ByRefEvent]
public record struct AttemptScanalyzerScanEvent(EntityUid Target, EntityUid User, bool Cancelled = false);

/// <summary>
/// Raised on the scanalyzer entity once a scan has finished.
/// </summary>
[ByRefEvent]
public record struct ScanalyzerScanFinishedEvent(EntityUid Target, EntityUid User);
