using Content.Server.Research.Systems;
using Content.Trauma.Shared.AER;


namespace Content.Trauma.Server.AER;


public sealed partial class AnomalousEntityContainmentSystem : EntitySystem
{
    [Dependency] private ResearchSystem _research = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnomalousEntityComponent, AerBehaviourEvent>(OnAerBehaviourEvent);
    }


    private void OnAerBehaviourEvent(Entity<AnomalousEntityComponent> ent, ref AerBehaviourEvent args)
    {
        if (ent.Comp is not { } anomalousEntityComp)
            return;

        if (ent.Comp.ConnectedContainment != null)
        {
            if (!_research.TryGetClientServer((EntityUid) ent.Comp.ConnectedContainment, out var server, out var serverComponent))
                return;

            if (server != null)
            {
                _research.ModifyServerPoints(server.Value, (int) ent.Comp.ResearchOnBehaviour, serverComponent);

            }
        }
    }

}

