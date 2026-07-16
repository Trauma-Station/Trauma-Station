// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Coordinates;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Research.Components;

namespace Content.Trauma.Shared.AER;

public sealed partial class AnomalousEntityContainmentSystem : EntitySystem
{
    [Dependency] private AnomalousEntitySystem _aerEntity = default!;
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
    private void OnContainmentShutdown(Entity<AnomalousEntityContainmentComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.AnomalousEntity is not { } entity)
            return;

        if (!TryComp<AnomalousEntityComponent>(entity, out var comp))
            return;

        comp.Contained = false;
        Dirty(entity, comp);
        comp.ConnectedContainment = null;
    }

    //sets id gear and contained aer on interaction with aer scanner
    private void OnAnomalousContainmentInteractUsing(Entity<AnomalousEntityContainmentComponent> ent, ref InteractUsingEvent args)
    {
        if (ent.Comp.AnomalousEntity != null ||
            !TryComp<AnomalousEntityScannerComponent>(args.Used, out var scanner) ||
            scanner.ScannedAER is not { } anomalousEntity)
        {
            return;
        }

        if (!TryComp<AnomalousEntityComponent>(anomalousEntity, out var anomalousEntityComponent) || anomalousEntityComponent.ConnectedContainment != null)
            return;

        ent.Comp.AnomalousEntity = scanner.ScannedAER;
        ent.Comp.Linked = true;
        Dirty(ent);

        anomalousEntityComponent.ConnectedContainment = ent.Owner;
        TryComp<AnomalousEntityComponent>(ent.Comp.AnomalousEntity, out var aer);
        if (aer != null && aer.IDGear.HasValue)
        {
            ent.Comp.IDGear = aer.IDGear;
        }

        _popup.PopupEntity(Loc.GetString("anomaly-vessel-component-anomaly-assigned"), ent.Owner);
    }

    /*stolen code from anom vessels*/
    private void OnExamined(EntityUid uid, AnomalousEntityContainmentComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushText(component.Linked == false
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
                args.Points += (int) (_aerEntity.GetAnomalousEntityPointValue(anomalousEntity) * component.PointMultiplier);
        }
        else
        {
            comp.Contained = false;
        }
    }

    //spawns I.D. gear on anom behaviour
    private void OnAerBehaviourSpawnGear(Entity<AnomalousEntityComponent> ent, ref AerBehaviourSpawnGearEvent args)
    {
        if (ent.Comp.ConnectedContainment == null || ent.Comp.ConnectedContainment == null)
            return;

        if (ent.Comp.ConnectedContainment is not { } aerContainmentId)
            return;

        //spawn I.D. Gear
        if (TryComp<AnomalousEntityContainmentComponent>(aerContainmentId, out var aerSensor))
        {
            if (ent.Comp.Contained)
            {
                PredictedSpawnAtPosition(aerSensor.IDGear, Transform(aerContainmentId).Coordinates);
            }
        }
    }
}
