using Content.Shared.IdentityManagement;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared._Shitcode.Heretic.Systems;
using Content.Shared.FixedPoint;

namespace Content.Server.Heretic.EntitySystems;

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

        _accumulator += frameTime;

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
