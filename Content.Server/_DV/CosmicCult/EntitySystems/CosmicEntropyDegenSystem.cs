using Content.Shared._DV.CosmicCult.Components;
using Robust.Shared.Timing;
using Content.Shared.Damage.Systems;
using Content.Medical.Common.Targeting;

namespace Content.Server._DV.CosmicCult.EntitySystems;

/// <summary>
/// Makes the person with this component take damage over time.
/// Used for status effect.
/// </summary>
public sealed partial class CosmicEntropyDegenSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CosmicDegenComponent, ComponentStartup>(OnStartup);
    }


    private void OnStartup(EntityUid uid, CosmicDegenComponent comp, ref ComponentStartup args)
    {
        comp.CheckTimer = _timing.CurTime + comp.CheckWait;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var degenQuery = EntityQueryEnumerator<CosmicDegenComponent>();
        while (degenQuery.MoveNext(out var uid, out var component))
        {
            if (_timing.CurTime < component.CheckTimer)
                continue;

            component.CheckTimer = _timing.CurTime + component.CheckWait;
            _damageable.TryChangeDamage(uid, component.Degen, true, false, targetPart: TargetBodyPart.All);
        }
    }
}
