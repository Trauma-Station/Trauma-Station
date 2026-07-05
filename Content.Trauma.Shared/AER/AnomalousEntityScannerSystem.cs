using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Anomaly;
using Content.Shared.Popups;
using Content.Shared.Examine;
using Content.Shared.Anomaly.Components;

using Robust.Shared.Audio.Systems;
using System.ComponentModel;

namespace Content.Trauma.Shared.AER;

public sealed partial class AnomalousEntityScannerSystem : EntitySystem
{
    //[Dependency] private ResearchSystem _research = default!;

    [Dependency] private AnomalousEntitySystem _anomaly = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    /*public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalySeverityChangedEvent>(OnScannerAnomalySeverityChanged);
        SubscribeLocalEvent<AnomalyStabilityChangedEvent>(OnScannerAnomalyStabilityChanged);
        SubscribeLocalEvent<AnomalyHealthChangedEvent>(OnScannerAnomalyHealthChanged);
        SubscribeLocalEvent<AnomalyBehaviorChangedEvent>(OnScannerAnomalyBehaviorChanged);

        Subs.BuiEvents<AnomalyScannerComponent>(
            AnomalyScannerUiKey.Key,
            subs => subs.Event<BoundUIOpenedEvent>(OnScannerUiOpened)
        );
    }*/

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalousEntityScannerComponent, ScannerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<AnomalousEntityScannerComponent, AfterInteractEvent>(OnScannerAfterInteract);
        SubscribeLocalEvent<AnomalousEntityScannerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<AnomalousEntityComponent, ComponentShutdown>(OnAerShutdown);
    }

    /// <summary> Updates device with passed anomaly data. </summary>
    public void UpdateScannerWithNewAnomaly(EntityUid scanner, EntityUid anomaly, AnomalousEntityScannerComponent? scannerComp = null, AnomalousEntityComponent? anomalyComp = null)
    {
        if (!Resolve(scanner, ref scannerComp) || !Resolve(anomaly, ref anomalyComp))
            return;

        scannerComp.ScannedAER = anomaly;
        //UpdateScannerUi(scanner, scannerComp);

        //TryComp<AppearanceComponent>(scanner, out var appearanceComp);
        //TryComp<SecretDataAnomalyComponent>(anomaly, out var secretDataComp);

        //Appearance.SetData(scanner, AnomalyScannerVisuals.HasAnomaly, true, appearanceComp);

        /*var stability = _secretData.IsSecret(anomaly, AnomalySecretData.Stability, secretDataComp)
            ? AnomalyStabilityVisuals.Stable
            : _anomaly.GetStabilityVisualOrStable((anomaly, anomalyComp));
        Appearance.SetData(scanner, AnomalyScannerVisuals.AnomalyStability, stability, appearanceComp);*/

        /*var severity = _secretData.IsSecret(anomaly, AnomalySecretData.Severity, secretDataComp)
            ? 0
            : anomalyComp.Severity;
        Appearance.SetData(scanner, AnomalyScannerVisuals.AnomalySeverity, severity, appearanceComp);*/
    }

    /// <summary> Update scanner interface. </summary>
    /*public void UpdateScannerUi(EntityUid uid, AnomalyScannerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        TimeSpan? nextPulse = null;
        if (TryComp<AnomalyComponent>(component.ScannedAnomaly, out var anomalyComponent))
            nextPulse = anomalyComponent.NextPulseTime;

        var state = new AnomalyScannerUserInterfaceState(_anomaly.GetScannerMessage(component), nextPulse);
        UI.SetUiState(uid, AnomalyScannerUiKey.Key, state);
    }*/

    /// <inheritdoc />
    /*public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var anomalyQuery = EntityQueryEnumerator<AnomalyComponent>();
        while (anomalyQuery.MoveNext(out var ent, out var anomaly))
        {
            var secondsUntilNextPulse = (anomaly.NextPulseTime - Timing.CurTime).TotalSeconds;
            UpdateScannerPulseTimers((ent, anomaly),  secondsUntilNextPulse);
        }
    }*/

    private void OnDoAfter(EntityUid uid, AnomalousEntityScannerComponent component, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        //base.OnDoAfter(uid, component, args);

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

    private void OnAerShutdown(Entity<AnomalousEntityComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp is not { } anomalousEntityComp)
            return;
        var query = EntityQueryEnumerator<AnomalousEntityScannerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ScannedAER != ent.Owner)
                continue;

            component.ScannedAER = null;
        }
    }





    /*private void OnScannerAnomalyHealthChanged(ref AnomalyHealthChangedEvent args)
    {
        var query = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ScannedAnomaly != args.Anomaly)
                continue;

            UpdateScannerUi(uid, component);
        }
    }*/

    /*private void OnScannerUiOpened(EntityUid uid, AnomalyScannerComponent component, BoundUIOpenedEvent args)
    {
        UpdateScannerUi(uid, component);
    }*/

    /*private void OnScannerAnomalySeverityChanged(ref AnomalySeverityChangedEvent args)
    {
        var severity = _secretData.IsSecret(args.Anomaly, AnomalySecretData.Severity) ? 0 : args.Severity;
        var query = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ScannedAnomaly != args.Anomaly)
                continue;

            UpdateScannerUi(uid, component);
            Appearance.SetData(uid, AnomalyScannerVisuals.AnomalySeverity, severity);
        }
    }*/

    /*private void OnScannerAnomalyStabilityChanged(ref AnomalyStabilityChangedEvent args)
    {
        var stability = _secretData.IsSecret(args.Anomaly, AnomalySecretData.Stability)
            ? AnomalyStabilityVisuals.Stable
            : _anomaly.GetStabilityVisualOrStable(args.Anomaly);
        var query = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ScannedAnomaly != args.Anomaly)
                continue;

            UpdateScannerUi(uid, component);
            Appearance.SetData(uid, AnomalyScannerVisuals.AnomalyStability, stability);
        }
    }*/


    /*private void UpdateScannerPulseTimers(Entity<AnomalyComponent> anomalyEnt, double secondsUntilNextPulse)
    {
        if (secondsUntilNextPulse > 5)
            return;

        var rounded = Math.Max(0, (int)Math.Ceiling(secondsUntilNextPulse));

        var scannerQuery = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (scannerQuery.MoveNext(out var scannerUid, out var scanner))
        {
            if (scanner.ScannedAnomaly != anomalyEnt)
                continue;

            Appearance.SetData(scannerUid, AnomalyScannerVisuals.AnomalyNextPulse, rounded);
        }
    }*/

}