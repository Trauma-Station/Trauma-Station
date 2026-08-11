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

    /// <summary>
    /// Starts the scanning do-after. Does not check if the scan should happen, use <see cref="CanScan"/> before calling this.
    /// </summary>
    public void StartScan(Entity<ScanalyzerComponent> ent, EntityUid user, EntityUid target)
    {
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, ent.Comp.ScanDuration, new ScanalyzerScanDoAfterEvent(), ent.Owner, target, ent.Owner)
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

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<StealTargetComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;
        if (!TryComp<ScanalyzerComponent>(args.Used, out var scanalyzer))
            return;
        if (!CanScan((args.Used, scanalyzer), ent))
            return;
        if (HasComp<ScanalyzerRequiresPowerComponent>(ent.Owner) && !_power.IsPowered(ent.Owner))
            return;

        StartScan((args.Used, scanalyzer), args.User, ent.Owner);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnScan(Entity<ScanalyzerComponent> ent, ref ScanalyzerScanDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is null)
            return;
        if (!TryComp<StealTargetComponent>(args.Target, out var steal))
            return;
        if (!CanScan(ent, (args.Target.Value, steal)))
            return;

        ent.Comp.Used = true;
        Dirty(ent);
        _popup.PopupClient(Loc.GetString("scanalyzer-popup-used"), args.User, PopupType.Medium);

        AfterScan(ent, args.User, steal.StealGroup);
        var ev = new ScanalyzerScanFinishedEvent(args.Target.Value, args.User);
        RaiseLocalEvent(ent, ref ev);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<ScanalyzerComponent> ent, ref ExaminedEvent args)
    {
        if (!_proto.Resolve(ent.Comp.StealTarget, out var target))
            return;
        args.PushMarkup(Loc.GetString("scanalyzer-examine-steal-target", ("target", Loc.GetString(target.Name))));
        args.PushMarkup(ent.Comp.Used
            ? Loc.GetString("scanalyzer-examine-used")
            : Loc.GetString("scanalyzer-examine-not-used"));
    }
}

/// <summary>
/// Do after even for scanning an item.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ScanalyzerScanDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// Raised on the scanalyzer entity once a scan has finished.
/// </summary>
[ByRefEvent]
public record struct ScanalyzerScanFinishedEvent(EntityUid Target, EntityUid User);
