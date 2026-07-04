using Content.Shared.Interaction;
using Content.Shared.Examine;
using Content.Shared.Anomaly.Components;
using Content.Shared.Research.Components;
using Content.Shared.Popups;
using Content.Trauma.Shared.AER;

namespace Content.Trauma.Shared.AER;

public sealed partial class AnomalousEntityContainmentSystem : EntitySystem
{
    [Dependency] private AnomalousEntitySystem _anomalousEntitySystem = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeAERContainment();
    }

    private void InitializeAERContainment()
    {
        SubscribeLocalEvent<AnomalousEntityContainmentComponent, ComponentShutdown>(OnContainmentShutdown);
        SubscribeLocalEvent<AnomalousEntityContainmentComponent, InteractUsingEvent>(OnAnomalousContainmentInteractUsing);
        SubscribeLocalEvent<AnomalousEntityContainmentComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<AnomalousEntityContainmentComponent, ResearchServerGetPointsPerSecondEvent>(OnAnomalousContainmentGetPointsPerSecond);
        SubscribeLocalEvent<AnomalyShutdownEvent>(OnAnomalousContainmentShutdown);
        SubscribeLocalEvent<AnomalousEntityComponent, AerBehaviourSpawnGearEvent>(OnAerBehaviourSpawnGear);
    }

    private void OnContainmentShutdown(EntityUid uid, AnomalousEntityContainmentComponent component, ComponentShutdown args)
    {
        if (component.AnomalousEntity is not { } anomalousEntity)
            return;

        if (!TryComp<AnomalousEntityComponent>(anomalousEntity, out var anomalousEntityComp))
            return;

        anomalousEntityComp.Contained = false;
    }

    private void OnAnomalousContainmentInteractUsing(EntityUid uid, AnomalousEntityContainmentComponent component, InteractUsingEvent args)
    {
        if (component.AnomalousEntity != null ||
            !TryComp<AnomalousEntityScannerComponent>(args.Used, out var scanner) ||
            scanner.ScannedAER is not { } anomalousEntity)
        {
            return;
        }

        if (!TryComp<AnomalousEntityComponent>(anomalousEntity, out var anomalousEntityComponent) || anomalousEntityComponent.ConnectedContainment != null)
            return;

        component.AnomalousEntity = scanner.ScannedAER;
        anomalousEntityComponent.ConnectedContainment = uid;
        TryComp<AnomalousEntityComponent>(component.AnomalousEntity, out var aer);//move this shit to linking scan thing and place extra variable in entity containment
        if (aer != null && aer.IDGear.HasValue)
        {
            component.IDGear = aer.IDGear;
        }
        //_radiation.SetSourceEnabled(uid, true);//no rads for now
        //UpdateVesselAppearance(uid,  component);//todo different apperances
        _popup.PopupEntity(Loc.GetString("anomaly-vessel-component-anomaly-assigned"), uid);
    }

    private void OnExamined(EntityUid uid, AnomalousEntityContainmentComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushText(component.AnomalousEntity == null
            ? Loc.GetString("anomaly-vessel-component-not-assigned")
            : Loc.GetString("anomaly-vessel-component-assigned"));
    }

    private void OnAnomalousContainmentGetPointsPerSecond(EntityUid uid, AnomalousEntityContainmentComponent component, ref ResearchServerGetPointsPerSecondEvent args)
    {
        if (component.AnomalousEntity is not { } anomalousEntity)
            return;

        args.Points += (int) (_anomalousEntitySystem.GetAnomalousEntityPointValue(anomalousEntity) * component.PointMultiplier);
    }

    private void OnAnomalousContainmentShutdown(ref AnomalyShutdownEvent args)
    {
        var query = EntityQueryEnumerator<AnomalousEntityContainmentComponent>();
        while (query.MoveNext(out var ent, out var component))
        {
            if (args.Anomaly != component.AnomalousEntity)
                continue;

            component.AnomalousEntity = null;
            component.IDGear = null;
            //UpdateVesselAppearance(ent,  component); to do appearance
            //_radiation.SetSourceEnabled(ent, false); no rads

            //if (!args.Supercritical)//no supercritical so no explosion either
            //    continue;
            //_explosion.TriggerExplosive(ent);
        }
    }

    private void OnAerBehaviourSpawnGear(Entity<AnomalousEntityComponent> ent, ref AerBehaviourSpawnGearEvent args)
    {
        if (ent.Comp is not { } anomalousEntityComp)
            return;

        if (ent.Comp.ConnectedContainment == null || ent.Comp.ConnectedContainment == null)
            return;

        var aerContainmentId = ent.Comp.ConnectedContainment;
        //spawn I.D. Gear
        var query = EntityQueryEnumerator<AnomalousEntityContainmentComponent>();
        while (query.MoveNext(out var aerSensor, out var component))
        {
            if (aerSensor == aerContainmentId)
            {
                PredictedSpawnAtPosition(component.IDGear, Transform(aerSensor).Coordinates);
            }
        }
    }
}