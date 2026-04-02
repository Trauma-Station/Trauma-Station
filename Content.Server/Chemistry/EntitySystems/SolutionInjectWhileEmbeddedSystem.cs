// <Trauma>
using Robust.Shared.Physics.Components;
// </Trauma>
using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.Events;
using Content.Shared.Projectiles;
using Robust.Shared.Timing;

namespace Content.Server.Chemistry.EntitySystems;

/// <summary>
/// System for handling injecting into an entity while a projectile is embedded.
/// </summary>
public sealed class SolutionInjectWhileEmbeddedSystem : EntitySystem
{
	[Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionInjectWhileEmbeddedComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<SolutionInjectWhileEmbeddedComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _gameTiming.CurTime + ent.Comp.UpdateInterval;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SolutionInjectWhileEmbeddedComponent, EmbeddableProjectileComponent>();
        while (query.MoveNext(out var uid, out var injectComponent, out var projectileComponent))
        {
            if (_gameTiming.CurTime < injectComponent.NextUpdate)
                continue;

            injectComponent.NextUpdate += injectComponent.UpdateInterval;

            // <Goobstation> Goobstation - Shot syringes injecting over time
            if(projectileComponent.EmbeddedIntoUid == null) // check if we should reset state,
            {
                if (TryComp<PhysicsComponent>(uid, out var physics) && physics.BodyStatus != BodyStatus.InAir) // don't reset in-flight things
                    injectComponent.Injections = 0;
                continue;
            }

            if ((injectComponent.Injections ?? 0) >= injectComponent.MaxInjections)
                continue;

            var attemptEv = new InjectOverTimeAttemptEvent(projectileComponent.EmbeddedIntoUid.Value);
            RaiseLocalEvent(uid, ref attemptEv);
            if (attemptEv.Cancelled)
                continue;

            if (injectComponent.Injections != null)
                injectComponent.Injections += 1;
            // </Goobstation>

            var ev = new InjectOverTimeEvent(projectileComponent.EmbeddedIntoUid.Value);
            RaiseLocalEvent(uid, ref ev);

        }
    }
}
