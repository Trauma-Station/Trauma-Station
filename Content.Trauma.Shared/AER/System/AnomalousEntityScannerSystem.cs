// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Anomaly;
using Content.Shared.Examine;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;

using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.AER;

public sealed partial class AnomalousEntityScannerSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    /// <summary>
    /// Updates device with passed anomaly data.
    /// </summary>
    public void UpdateScannerWithNewAnomaly(EntityUid scanner, EntityUid anomaly, AnomalousEntityScannerComponent? scannerComp = null, AnomalousEntityComponent? anomalyComp = null)
    {
        if (!Resolve(scanner, ref scannerComp) || !Resolve(anomaly, ref anomalyComp))
            return;

        scannerComp.ScannedAER = anomaly;
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<AnomalousEntityScannerComponent> ent, ref ScannerDoAfterEvent args)
    {

        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        _audio.PlayPredicted(ent.Comp.CompleteSound, ent.Owner, args.User);
        _popup.PopupPredicted(Loc.GetString("anomaly-scanner-component-scan-complete"), ent.Owner, args.User);

        UpdateScannerWithNewAnomaly(ent.Owner, target, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnScannerAfterInteract(Entity<AnomalousEntityScannerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target)
            return;

        if (!HasComp<AnomalousEntityComponent>(target))
            return;

        if (!args.CanReach)
            return;

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            args.User,
            ent.Comp.ScanDoAfterDuration,
            new ScannerDoAfterEvent(),
            ent.Owner,
            target: target,
            used: ent.Owner
        )
        {
            DistanceThreshold = 2f
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<AnomalousEntityScannerComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushText(ent.Comp.ScannedAER is not { } scannedAer
            ? Loc.GetString("anomaly-vessel-component-not-assigned")
            : (Loc.GetString("anomaly-vessel-component-assigned") + " it contains a scan of " + Name(scannedAer)));
    }

}
