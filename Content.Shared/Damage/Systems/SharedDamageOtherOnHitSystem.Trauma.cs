using Content.Shared.Administration.Logs;
using Content.Shared.Camera;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Shared.Mobs.Components;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Shared.Damage.Systems;

/// <summary>
/// Trauma - shitcode moved out of server literally no reason for it to be there
/// </summary>
public abstract partial class SharedDamageOtherOnHitSystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;

    private List<EntityUid> _target = new(1);

    private EntityQuery<MobStateComponent> _mobQuery;

    private void InitializeTrauma()
    {
        _mobQuery = GetEntityQuery<MobStateComponent>();

        SubscribeLocalEvent<DamageOtherOnHitComponent, ThrowDoHitEvent>(OnDoHit);
    }

    private void OnDoHit(EntityUid uid, DamageOtherOnHitComponent component, ThrowDoHitEvent args)
    {
        if (TerminatingOrDeleted(args.Target))
            return;

        if (args.Target == args.Component.Thrower) // Goobstation - Mjolnir
            return;

        var targetPart = args.Component.Thrower is {} thrower
            ? _gun.GetTargetPart(thrower, args.Target)
            : null;
        var dmg = _damageable.ChangeDamage(args.Target, component.Damage * _damageable.UniversalThrownDamageModifier, component.IgnoreResistances,
            targetPart: targetPart, origin: args.Component.Thrower);

        // <Goob>
        // For stuff that cares about it being attacked.
        var attackedEvent = new AttackedEvent(args.Thrown, uid, args.Target.ToCoordinates());
        RaiseLocalEvent(args.Target, attackedEvent);
        // </Goob>

        // Log damage only for mobs. Useful for when people throw spears at each other, but also avoids log-spam when explosions send glass shards flying.
        if (_mobQuery.HasComp(args.Target))
            _adminLogger.Add(LogType.ThrowHit, $"{ToPrettyString(args.Target):target} received {dmg.GetTotal():damage} damage from collision");

        if (!dmg.Empty && _net.IsClient) // prevent double flash
        {
            _target.Clear();
            _target.Add(args.Target);
            _color.RaiseEffect(Color.Red, _target, Filter.Pvs(args.Target, entityManager: EntityManager));
        }

        _gun.PlayImpactSound(args.Target, dmg, null, false);
        if (TryComp<PhysicsComponent>(uid, out var body) && body.LinearVelocity.LengthSquared() > 0f)
        {
            var direction = body.LinearVelocity.Normalized();
            _recoil.KickCamera(args.Target, direction);
        }
    }
}
