using Content.Shared.Slippery;
using Content.Trauma.Shared.Wizard;

namespace Content.Trauma.Shared.AER;

public sealed partial class AerBehaviourWailingHorseSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AerBehaviourWailingHorseComponent, RepulseEvent>(OnWail);
    }


    /// <summary>
    /// raises the research and id gear event on the horse wailing
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void OnWail(Entity<AerBehaviourWailingHorseComponent> ent, ref RepulseEvent args)
    {
        var spawnEvent = new AerBehaviourSpawnGearEvent(ent.Owner);
        RaiseLocalEvent(ent.Owner, ref spawnEvent);
        var researchEvent = new AerBehaviourAddResearchEvent(ent.Owner);
        RaiseLocalEvent(ent.Owner, ref researchEvent);

    }

}