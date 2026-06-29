using Content.Goobstation.Shared.Wraith.Components.Mobs;
using Content.Shared.Research.Systems;

namespace Content.Trauma.Shared.AER;

public sealed partial class AnomalousEntitySystem : EntitySystem
{
    [Dependency] private SharedResearchSystem _research = default!;


    public override void Initialize()
    {
        base.Initialize();
        //SubscribeLocalEvent<AnomalousEntityComponent, AerBehaviourEvent>(OnAerBehaviourEvent);
    }

    /*private void OnAerBehaviourEvent(Entity<AnomalousEntityComponent> ent, ref AerBehaviourEvent args)
    {
        //shit that give research
        

        //shit that spawns I.D. Gear
    }*/

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
