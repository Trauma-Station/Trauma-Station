// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Objectives;
using Content.Shared.Objectives.Components;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Trigger.Systems;

namespace Content.Trauma.Shared.JobListings;

/// <summary>
/// System facilitating scanning grand theft items to complete progtot objectives.
/// The list of scanned grand theft items is stored on the traitor's mind via the <see cref="ScanalyzerMindArchiveComponent"/>
/// </summary>
public abstract partial class SharedScanalyzerSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedJobListingsSystem _jobs = default!;
    [Dependency] private TriggerSystem _trigger = default!;

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

    /// <summary>
    /// Determines if the inputted mind has scanned the grand theft item.
    /// </summary>
    public bool IsScanned(Entity<MindComponent> mind, ProtoId<StealTargetGroupPrototype> target)
    {
        if (!TryComp<ScanalyzerMindArchiveComponent>(mind.Owner, out var archive))
            return false;
        return archive.ScannedStealTargetGroups.Contains(target);
    }

    /// <summary>
    /// Register a grand theft item as scanned.
    /// </summary>
    public void RegisterScan(Entity<MindComponent> mind, ProtoId<StealTargetGroupPrototype> target)
    {
        var archive = EnsureComp<ScanalyzerMindArchiveComponent>(mind.Owner);
        if (archive.ScannedStealTargetGroups.Contains(target))
            return;
        archive.ScannedStealTargetGroups.Add(target);
        Dirty(mind.Owner, archive);
    }

    [SubscribeLocalEvent]
    private void OnScan(Entity<TriggerOnScanComponent> ent, ref ScanalyzerScanFinishedEvent args)
    {
        _trigger.Trigger(ent.Owner, args.User, ent.Comp.KeyOut, false);
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

        if (!_mind.TryGetMind(args.User, out var mind, out var mindComp))
            return;
        RegisterScan((mind, mindComp), steal.StealGroup);
        _jobs.UpdateUi((mind, mindComp));
        var ev = new ScanalyzerScanFinishedEvent(args.Target.Value, args.User);
        RaiseLocalEvent(ent, ref ev);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<ScanalyzerComponent> ent, ref ExaminedEvent args)
    {
        if (!ProtoMan.Resolve(ent.Comp.StealTarget, out var target))
            return;
        args.PushMarkup(Loc.GetString("scanalyzer-examine-steal-target", ("target", Loc.GetString(target.Name))));
        args.PushMarkup(Loc.GetString($"scanalyzer-examine-{(ent.Comp.Used ? "used" : "not-used")}"));
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
