// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Timing;

namespace Content.Trauma.Server.GrantTemporatyComponent;

/// <summary>
/// Is there a better way to track this or is it really needs to be updated every frame?
/// Srver becouse for some reason it doesn't work on client side. idk why
/// Adds temporary components and removes them when their lifeSpan ends.
/// </summary>
public sealed partial class GrantTemporatyComponentSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    public void GrantTemporatyComponent(EntityUid uid, ComponentRegistry comps, TimeSpan lifeSpan)
    {
        var comp = EnsureComp<GrantTemporatyComponentComponent>(uid);
        comp.Components = comps; // for some reason set to Null on client side
        comp.DeleteAfter = lifeSpan + _timing.CurTime;

        //Log.Info($"AddComponents: {comp.Components}");
        EntityManager.AddComponents(uid, comp.Components);

        // TryComp<GrantTemporatyComponentComponent>(uid, out var c);
        // foreach (var co in c.Components){
        //     Log.Info($"Components: {c}");
        // }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        List<EntityUid> toRemove = [];

        var query = EntityQueryEnumerator<GrantTemporatyComponentComponent>();
        while (query.MoveNext(out var uid, out var comp)){
            if (_timing.CurTime >= comp.DeleteAfter){
                EntityManager.RemoveComponents(uid, comp.Components);
                toRemove.Add(uid);
            }
        }

        foreach (EntityUid ent in toRemove){
            RemComp<GrantTemporatyComponentComponent>(ent);
        }
    }
}