using Content.Shared.Interaction;
using Content.Shared.Examine;
using Content.Shared.Anomaly.Components;
using Content.Shared.Research.Components;
using Content.Shared.Popups;
using Content.Trauma.Shared.AER;
using Content.Shared.Coordinates;

namespace Content.Trauma.Shared.AER;

public sealed partial class AnomalousEntityContainmentSystem : EntitySystem
{
    [Dependency] private AnomalousEntitySystem _anomalousEntitySystem = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

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
        SubscribeLocalEvent<AnomalousEntityComponent, AerBehaviourSpawnGearEvent>(OnAerBehaviourSpawnGear);
    }

    //on containment component removal uncontains the aer and sets connected containment on the aer to null
    private void OnContainmentShutdown(EntityUid uid, AnomalousEntityContainmentComponent component, ComponentShutdown args)
    {
        if (component.AnomalousEntity is not { } anomalousEntity)
            return;

        if (!TryComp<AnomalousEntityComponent>(anomalousEntity, out var anomalousEntityComp))
            return;

        anomalousEntityComp.Contained = false;
        anomalousEntityComp.ConnectedContainment = null;
    }

    //sets id gear and contained aer on interaction with aer scanner
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
        TryComp<AnomalousEntityComponent>(component.AnomalousEntity, out var aer);
        if (aer != null && aer.IDGear.HasValue)
        {
            component.IDGear = aer.IDGear;
        }
        //_radiation.SetSourceEnabled(uid, true);//no rads for now
        //UpdateVesselAppearance(uid,  component);//todo different apperances
        _popup.PopupEntity(Loc.GetString("anomaly-vessel-component-anomaly-assigned"), uid);
    }

    /*stolen code from anom vessels*/
    private void OnExamined(EntityUid uid, AnomalousEntityContainmentComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushText(component.AnomalousEntity == null
            ? Loc.GetString("anomaly-vessel-component-not-assigned")
            : Loc.GetString("anomaly-vessel-component-assigned"));
    }

    /*adds the aer's research points per second when ResearchServerGetPointsPerSecondEvent gets called
      also sets containment status depending on distance from the sensor*/
    private void OnAnomalousContainmentGetPointsPerSecond(EntityUid uid, AnomalousEntityContainmentComponent component, ref ResearchServerGetPointsPerSecondEvent args)
    {
        if (component.AnomalousEntity is not { } anomalousEntity)
            return;
        if (!TryComp<AnomalousEntityComponent>(anomalousEntity, out var comp))
            return;


        if (_transform.InRange(anomalousEntity.ToCoordinates(), uid.ToCoordinates(), component.Range))
        {
            comp.Contained = true;
            if (comp.Active)
                args.Points += (int) (_anomalousEntitySystem.GetAnomalousEntityPointValue(anomalousEntity) * component.PointMultiplier);
        }
        else
        {
            comp.Contained = false;
        }
    }

    //spawns I.D. gear on anom behaviour 
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
            if (aerSensor == aerContainmentId && ent.Comp.Contained)
            {
                PredictedSpawnAtPosition(component.IDGear, Transform(aerSensor).Coordinates);
            }
        }
    }
}