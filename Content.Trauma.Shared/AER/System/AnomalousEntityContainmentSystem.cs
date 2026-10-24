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

    //on containment component removal uncontains the aer and sets connected containment on the aer to null
    [SubscribeLocalEvent]
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
    [SubscribeLocalEvent]
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

        _popup.PopupEntity(Loc.GetString("anomaly-vessel-component-anomaly-assigned"), ent.Owner);
    }

    /*stolen code from anom vessels*/
    [SubscribeLocalEvent]
    private void OnExamined(Entity<AnomalousEntityContainmentComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushText(ent.Comp.Linked == false
            ? Loc.GetString("anomaly-vessel-component-not-assigned")
            : Loc.GetString("anomaly-vessel-component-assigned"));
    }

    /*adds the aer's research points per second when ResearchServerGetPointsPerSecondEvent gets called
      also sets containment status depending on distance from the sensor*/
    [SubscribeLocalEvent]
    private void OnAnomalousContainmentGetPointsPerSecond(Entity<AnomalousEntityContainmentComponent> ent, ref ResearchServerGetPointsPerSecondEvent args)
    {
        if (ent.Comp.AnomalousEntity is not { } anomalousEntity)
            return;
        if (!TryComp<AnomalousEntityComponent>(anomalousEntity, out var comp))
            return;


        if (_transform.InRange(anomalousEntity.ToCoordinates(), ent.Owner.ToCoordinates(), ent.Comp.Range))
        {
            comp.Contained = true;
            var aer = new Entity<AnomalousEntityComponent>(anomalousEntity, comp);
            if (comp.Active)
                args.Points += (int) (_aerEntity.GetAnomalousEntityPointValue(aer) * ent.Comp.PointMultiplier);
        }
        else
        {
            comp.Contained = false;
        }
        Dirty(anomalousEntity, comp);
    }

    //spawns I.D. gear on anom behaviour
    [SubscribeLocalEvent]
    private void OnAerBehaviourSpawnGear(Entity<AnomalousEntityComponent> ent, ref AerBehaviourSpawnGearEvent args)
    {
        if (ent.Comp.ConnectedContainment is not { } aerContainmentId)
            return;

        if (ent.Comp.IdGear is not { } idGear)
            return;

        //spawn I.D. Gear
        if (TryComp<AnomalousEntityContainmentComponent>(aerContainmentId, out var aerSensor))
        {
            if (ent.Comp.Contained)
            {
                PredictedSpawnAtPosition(idGear, Transform(aerContainmentId).Coordinates);
            }
        }
    }
}
