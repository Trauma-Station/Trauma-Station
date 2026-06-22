// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Objectives;
using Content.Shared.Objectives.Components;
using Content.Shared.Popups;

namespace Content.Trauma.Shared.JobListings;

/// <summary>
/// System facilitating scanning grand theft items to complete progtot objectives.
/// The list of scanned grand theft items is stored on the traitor's mind via the <see cref="ScanalyzerMindArchiveComponent"/>
/// </summary>
public abstract partial class SharedScanalyzerSystem : EntitySystem
{
    [Dependency] protected IPrototypeManager _proto = default!;
    [Dependency] protected SharedDoAfterSystem _doAfter = default!;
    [Dependency] protected SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ScanalyzerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ScanalyzerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ScanalyzerComponent, ScanalyzerScanDoAfterEvent>(OnScan);
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
    public bool CanScan(Entity<ScanalyzerComponent> entity, EntityUid target)
    {
        if (entity.Comp.Used)
            return false;
        if (!TryComp<StealTargetComponent>(target, out var steal))
            return false;
        if (entity.Comp.StealTarget != steal.StealGroup)
            return false;
        return true;
    }

    protected virtual void AfterScan(Entity<ScanalyzerComponent> entity, EntityUid user, ProtoId<StealTargetGroupPrototype> target) {}

    private void OnInteractUsing(Entity<ScanalyzerComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;
        if (!CanScan(entity, args.Target))
            return;

        StartScan(entity, args.User, args.Target);
        args.Handled = true;
    }

    private void OnScan(Entity<ScanalyzerComponent> entity, ref ScanalyzerScanDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;
        if (!TryComp<StealTargetComponent>(args.Target, out var steal))
            return;

        entity.Comp.Used = true;
        Dirty(entity);
        _popup.PopupClient(Loc.GetString("scanalyzer-popup-used"), args.User, PopupType.Medium);

        AfterScan(entity, args.User, steal.StealGroup);
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

}

public sealed partial class ScanalyzerScanDoAfterEvent : SimpleDoAfterEvent;
