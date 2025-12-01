// <Trauma>
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
// </Trauma>
using Content.Shared.Damage.Systems;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanBasicDamageSystem : EntitySystem
{
    // <Trauma>
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    // </Trauma>
    [Dependency] private readonly DamageableSystem _damage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanBasicDamageComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanBasicDamageComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        // <Trauma> - put HitEntity into target
        if (args.Data.HitEntity is not {} target)
            return;

        var dmg = ent.Comp.Damage * _damage.UniversalHitscanDamageModifier;

        // <Trauma> - add targetPart and canBeCancelled
        var user = args.Data.Shooter ?? args.Data.Gun;
        var targetPart = _gun.GetTargetPart(
            user,
            _transform.GetMapCoordinates(user),
            _transform.GetMapCoordinates(target));
        if(!_damage.TryChangeDamage(target, dmg, out var damageDealt, origin: args.Data.Gun, targetPart: targetPart, canBeCancelled: true))
            return;
        // </Trauma>

        var damageEvent = new HitscanDamageDealtEvent
        {
            Target = target,
            DamageDealt = damageDealt,
        };
        // </Trauma>

        RaiseLocalEvent(ent, ref damageEvent);
    }
}
