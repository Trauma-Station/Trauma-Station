// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.AER;

public sealed partial class AnomalousEntitySystem : EntitySystem
{
    /// <summary>
    /// calculates the pointa value of the AER
    /// Can be null.
    /// </summary>
    public int GetAnomalousEntityPointValue(Entity<AnomalousEntityComponent> aer)
    {
        return aer.Comp.ResearchPerSecond;
    }

    /// <summary>
    /// removes references to the aer and it's id gear on scanners and containment sensors on anomalous entity component shutdown
    /// </summary>
    [SubscribeLocalEvent]
    private void OnAnomalousEntityShutdown(Entity<AnomalousEntityComponent> aer, ref ComponentShutdown args)
    {
        var queryContainment = EntityQueryEnumerator<AnomalousEntityContainmentComponent>();
        while (queryContainment.MoveNext(out var ent, out var component))
        {
            if (aer.Owner != component.AnomalousEntity)
                continue;

            component.AnomalousEntity = null;
            component.Linked = false;
            Dirty(ent, component);
        }

        var queryScanner = EntityQueryEnumerator<AnomalousEntityScannerComponent>();
        while (queryScanner.MoveNext(out var ent, out var component))
        {
            if (aer.Owner != component.ScannedAER)
                continue;

            component.ScannedAER = null;
        }
    }

    /// <summary>
    /// updates the active status on event (example not dead, fueled, powered, etc)
    /// </summary>
    [SubscribeLocalEvent]
    private void OnAerActiveUpdate(Entity<AnomalousEntityComponent> aer, ref AerUpdateActiveStatusEvent args)
    {
        var component = aer.Comp;
        component.Active = args.Active;
    }
}
