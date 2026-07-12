// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mobs;
using Content.Shared.Slippery;
using Content.Trauma.Shared.Wizard;

namespace Content.Trauma.Shared.AER;

public sealed partial class AerBehaviourWailingHorseSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AerBehaviourWailingHorseComponent, RepulseEvent>(OnWail);
        SubscribeLocalEvent<AerBehaviourWailingHorseComponent, MobStateChangedEvent>(OnMobStateChanged);
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

    private void OnDeath(Entity<AerBehaviourWailingHorseComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            var aerActiveEvent = new AerUpdateActiveStatusEvent(ent.Owner, false);
            RaiseLocalEvent(ent.Owner, ref aerActiveEvent);
        }
    }

    private void OnAlive(Entity<AerBehaviourWailingHorseComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
        {
            var aerActiveEvent = new AerUpdateActiveStatusEvent(ent.Owner, true);
            RaiseLocalEvent(ent.Owner, ref aerActiveEvent);
        }
    }

    private void OnMobStateChanged(Entity<AerBehaviourWailingHorseComponent> ent, ref MobStateChangedEvent args)
    {
        switch (args.NewMobState)
        {
            case MobState.Dead:
                OnDeath(ent, ref args);
                break;
            case MobState.Alive:
                OnAlive(ent, ref args);
                break;
            default:
                break;
        }
    }

}
