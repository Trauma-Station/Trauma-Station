using Content.Shared.Body;

namespace Content.Medical.Shared.Body;

public sealed class FragileOrganSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FragileOrganComponent, OrganGotRemovedEvent>(OnRemove);
    }

    private void OnRemove(Entity<FragileOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (!TerminatingOrDeleted(ent))
            PredictedQueueDel(ent);
    }
}
