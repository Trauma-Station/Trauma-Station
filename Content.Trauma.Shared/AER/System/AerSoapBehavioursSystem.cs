// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Slippery;

namespace Content.Trauma.Shared.AER;

public sealed partial class AerSoapBehavioursSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnomalousEntityComponent, SlipEvent>(OnSlip);
    }


    private void OnSlip(Entity<AnomalousEntityComponent> ent, ref SlipEvent args)
    {
        var spawnEvent = new AerBehaviourSpawnGearEvent(ent.Owner);
        RaiseLocalEvent(ent.Owner, ref spawnEvent);
        var researchEvent = new AerBehaviourAddResearchEvent(ent.Owner);
        RaiseLocalEvent(ent.Owner, ref researchEvent);

    }

}
