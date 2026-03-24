using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Trauma.Shared.Heretic.Components.Side;
using Content.Trauma.Shared.Heretic.Systems.Side;

namespace Content.Trauma.Server.Heretic.Systems;

public sealed class ShadowCloakSystem : SharedShadowCloakSystem
{
    [Dependency] private readonly IdentitySystem _identity = default!;

    private const float SustainedDamageReductionInterval = 1f;
    private float _accumulator;

    protected override void Startup(Entity<ShadowCloakedComponent> ent)
    {
        base.Startup(ent);

        _identity.QueueIdentityUpdate(ent);
    }

    protected override void Shutdown(Entity<ShadowCloakedComponent> ent)
    {
        base.Shutdown(ent);

        _identity.QueueIdentityUpdate(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulator += frameTime; // TODO TimeSpan

        if (_accumulator < SustainedDamageReductionInterval)
            return;

        _accumulator = 0f;

        var shadowCloakedQuery = EntityQueryEnumerator<ShadowCloakEntityComponent>();
        while (shadowCloakedQuery.MoveNext(out _, out var comp))
        {
            comp.SustainedDamage =
                FixedPoint2.Max(comp.SustainedDamage - comp.SustainedDamageReductionRate, FixedPoint2.Zero);
        }
    }
}
