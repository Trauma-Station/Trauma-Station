// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Wraith.Components.Mobs;
using Content.Shared.Research.Systems;

namespace Content.Trauma.Shared.AER;

public sealed partial class AnomalousEntitySystem : EntitySystem
{
    [Dependency] private SharedResearchSystem _research = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnomalousEntityComponent, ComponentShutdown>(OnAnomalousEntityShutdown);
        SubscribeLocalEvent<AnomalousEntityComponent, AerUpdateActiveStatusEvent>(OnAerActiveUpdate);
    }

    /// <summary>
    /// calculates the pointa value of the AER
    /// Can be null.
    /// </summary>
    public int GetAnomalousEntityPointValue(EntityUid anomalousEntity, AnomalousEntityComponent? component = null)
    {
        if (!Resolve(anomalousEntity, ref component, false))
            return 0;

        return component.ResearchPerSecond;
    }

    /// <summary>
    /// removes references to the aer and it's id gear on scanners and containment sensors on anomalous entity component shutdown
    /// </summary>
    private void OnAnomalousEntityShutdown(Entity<AnomalousEntityComponent> aer, ref ComponentShutdown args)
    {
        var queryContainment = EntityQueryEnumerator<AnomalousEntityContainmentComponent>();
        while (queryContainment.MoveNext(out var ent, out var component))
        {
            if (aer.Owner != component.AnomalousEntity)
                continue;

            component.AnomalousEntity = null;
            component.IDGear = null;
        }

        var queryScanner = EntityQueryEnumerator<AnomalousEntityScannerComponent>();
        while (queryScanner.MoveNext(out var ent, out var component))
        {
            if (aer.Owner != component.ScannedAER)
                continue;

            component.ScannedAER = null;
        }
    }

    private void OnAerActiveUpdate(Entity<AnomalousEntityComponent> aer, ref AerUpdateActiveStatusEvent args)
    {
        var component = aer.Comp;
        component.Active = args.Active;
    }
}
