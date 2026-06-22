using Content.Shared.Research.Systems;

namespace Content.Trauma.Shared.AER;

public sealed partial class AnomalousEntitySystem : EntitySystem
{
    //[Dependency] private ResearchSystem _research = default!;

    /// <summary>
    /// calculates the pointa value of the AER
    /// Can be null.
    /// </summary>
    public int GetAnomalousEntityPointValue(EntityUid anomalousEntity, AnomalousEntityComponent? component = null)
    {
        if (!Resolve(anomalousEntity, ref component, false))
            return 0;

        //var multiplier = 1f;

        return (int) component.ResearchPerSecond;
    }
}
