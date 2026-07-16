// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mobs;
using Content.Trauma.Shared.Wizard;

namespace Content.Trauma.Shared.AER;

public sealed partial class AerHorseSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AerHorseComponent, RepulseEvent>(OnWail);
        SubscribeLocalEvent<AerHorseComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    /// <summary>
    /// raises the research and id gear event on the horse wailing
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void OnWail(Entity<AerHorseComponent> ent, ref RepulseEvent args)
    {
        var spawnEvent = new AerBehaviourSpawnGearEvent(ent.Owner);
        RaiseLocalEvent(ent.Owner, ref spawnEvent);
        var researchEvent = new AerBehaviourAddResearchEvent(ent.Owner);
        RaiseLocalEvent(ent.Owner, ref researchEvent);
    }

    /// <summary>
    /// handling of the aer active status for mobs it determines if aer is healty enough to produce rd points
    /// </summary>
    private void OnMobStateChanged(Entity<AerHorseComponent> ent, ref MobStateChangedEvent args)
    {
        switch (args.NewMobState)
        {
            case MobState.Dead:
                var deadEvent = new AerUpdateActiveStatusEvent(ent.Owner, false);
                RaiseLocalEvent(ent.Owner, ref deadEvent);
                break;
            case MobState.Alive:
                var aliveEvent = new AerUpdateActiveStatusEvent(ent.Owner, true);
                RaiseLocalEvent(ent.Owner, ref aliveEvent);
                break;
            default:
                break;
        }
    }

}
