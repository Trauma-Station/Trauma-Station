// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;
using Content.Trauma.Shared.Heretic.Systems;

namespace Content.Trauma.Server.Heretic.Systems;

public sealed class HereticCombatMarkSystem : SharedHereticCombatMarkSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticCombatMarkComponent, ComponentStartup>(OnStart);
        SubscribeLocalEvent<HereticCombatMarkComponent, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<HereticCosmicMarkComponent, ComponentRemove>(OnCosmicRemove);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;

        foreach (var comp in EntityQuery<HereticCombatMarkComponent>())
        {
            if (now > comp.Timer)
                RemComp(comp.Owner, comp);
        }
    }

    private void OnStart(Entity<HereticCombatMarkComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Timer == TimeSpan.Zero)
            ent.Comp.Timer = Timing.CurTime + TimeSpan.FromSeconds(ent.Comp.DisappearTime);
    }

    private void OnRemove(Entity<HereticCombatMarkComponent> ent, ref ComponentRemove args)
    {
        if (TerminatingOrDeleted(ent.Owner))
            return;

        RemComp<HereticCosmicMarkComponent>(ent.Owner);
    }

    private void OnCosmicRemove(Entity<HereticCosmicMarkComponent> ent, ref ComponentRemove args)
    {
        if (TerminatingOrDeleted(ent.Comp.CosmicDiamondUid))
            return;

        Del(ent.Comp.CosmicDiamondUid);
    }
}
