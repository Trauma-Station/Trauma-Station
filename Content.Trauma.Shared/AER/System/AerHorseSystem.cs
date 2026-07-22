// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mobs;
using Content.Trauma.Shared.Wizard;

namespace Content.Trauma.Shared.AER;

public sealed partial class AerHorseSystem : EntitySystem
{
    /// <summary>
    /// raises the research and id gear event on the horse wailing
    /// </summary>
    [SubscribeLocalEvent]
    private void OnWail(Entity<AerHorseComponent> ent, ref RepulseEvent args)
    {
        var spawnEvent = new AerBehaviourSpawnGearEvent(ent.Owner);
        RaiseLocalEvent(ent.Owner, ref spawnEvent);
        var researchEvent = new AerBehaviourAddResearchEvent(ent.Owner);
        RaiseLocalEvent(ent.Owner, ref researchEvent);
    }
}
