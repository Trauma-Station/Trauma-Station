using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Anomaly;
using Content.Shared.Popups;
using Content.Shared.Examine;

using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.AER;

public sealed partial class AnomalousEntityScannerSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalousEntityScannerComponent, ScannerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<AnomalousEntityScannerComponent, AfterInteractEvent>(OnScannerAfterInteract);
        SubscribeLocalEvent<AnomalousEntityScannerComponent, ExaminedEvent>(OnExamined);
    }

    /// <summary> Updates device with passed anomaly data. </summary>
    public void UpdateScannerWithNewAnomaly(EntityUid scanner, EntityUid anomaly, AnomalousEntityScannerComponent? scannerComp = null, AnomalousEntityComponent? anomalyComp = null)
    {
        if (!Resolve(scanner, ref scannerComp) || !Resolve(anomaly, ref anomalyComp))
            return;

        scannerComp.ScannedAER = anomaly;
    }

    private void OnDoAfter(EntityUid uid, AnomalousEntityScannerComponent component, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        _audio.PlayPredicted(component.CompleteSound, uid, args.User);
        _popup.PopupPredicted(Loc.GetString("anomaly-scanner-component-scan-complete"), uid, args.User);

        UpdateScannerWithNewAnomaly(uid, args.Args.Target.Value, component);
    }


    private void OnScannerAfterInteract(EntityUid uid, AnomalousEntityScannerComponent component, AfterInteractEvent args)
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
            component.ScanDoAfterDuration,
            new ScannerDoAfterEvent(),
            uid,
            target: target,
            used: uid
        )
        {
            DistanceThreshold = 2f
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnExamined(EntityUid uid, AnomalousEntityScannerComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;
        if (component.ScannedAER == null)
            return;

        args.PushText(component.ScannedAER == null
            ? Loc.GetString("anomaly-vessel-component-not-assigned")
            : (Loc.GetString("anomaly-vessel-component-assigned") + " it contains a scan of " + Name((EntityUid) component.ScannedAER)));
    }

}