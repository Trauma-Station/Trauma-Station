// <Trauma>
using Content.Medical.Common.Targeting;
using Content.Shared.Hands.EntitySystems;
using Content.Trauma.Common.Damage;
// </Trauma>
using Content.Shared.Damage.Components;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Damage.Systems;

public sealed partial class DamageOnHoldingSystem : EntitySystem
{
    // <Trauma>
    [Dependency] private SharedHandsSystem _hands = default!;
    // </Trauma>
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private DamageableSystem _damageableSystem = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageOnHoldingComponent, MapInitEvent>(OnMapInit);
    }

    public void SetEnabled(EntityUid uid, bool enabled, DamageOnHoldingComponent? component = null)
    {
        if (Resolve(uid, ref component) && component.Enabled != enabled) // Trauma - don't do anything if it's the same
        {
            component.Enabled = enabled;
            component.NextDamage = _timing.CurTime;
            Dirty(uid, component); // Trauma - dirty it bruh
        }
    }

    private void OnMapInit(EntityUid uid, DamageOnHoldingComponent component, MapInitEvent args)
    {
        component.NextDamage = _timing.CurTime;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<DamageOnHoldingComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!component.Enabled || component.NextDamage > _timing.CurTime)
                continue;
            if (_container.TryGetContainingContainer((uid, null, null), out var container))
            {
                // <Trauma>
                var holder = container.Owner;
                if (!_hands.IsHolding(holder, uid))
                    continue; // holy dogshit never checked what the container was...

                var attemptEv = new DamageOnHoldingAttemptEvent(uid);
                RaiseLocalEvent(holder, ref attemptEv);
                if (attemptEv.Cancelled)
                    continue;
                // </Trauma>
                _damageableSystem.TryChangeDamage(container.Owner, component.Damage, origin: uid,
                    targetPart: TargetBodyPart.Hands); // Trauma
            }
            component.NextDamage = _timing.CurTime + TimeSpan.FromSeconds(component.Interval);
        }
    }
}
