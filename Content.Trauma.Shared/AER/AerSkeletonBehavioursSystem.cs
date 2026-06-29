using Content.Shared.Slippery;

namespace Content.Trauma.Shared.AER;

public sealed partial class AerSkeletonBehavioursSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnomalousEntityComponent, SlipEvent>(OnSlip);
    }


    private void OnSlip(Entity<AnomalousEntityComponent> ent, ref SlipEvent args)
    {
        var ev = new AerBehaviourEvent(ent.Owner);
        RaiseLocalEvent(ent.Owner, ref ev);

    }

}