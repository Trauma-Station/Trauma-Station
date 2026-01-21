using Content.Lavaland.Common.Weapons.Marker;
using Content.Lavaland.Server.Pressure;
using Content.Lavaland.Shared.Weapons.Marker;
using Content.Shared._White.BackStab;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Marker;

namespace Content.Lavaland.Server.Weapons;

public sealed class DamageBoostOnMarkerSystem : EntitySystem
{
    [Dependency] private readonly BackStabSystem _backstab = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly PressureEfficiencyChangeSystem _pressure = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageBoostOnMarkerComponent, ApplyMarkerBonusEvent>(OnApplyMarkerBonus);
    }

    private void OnApplyMarkerBonus(Entity<DamageBoostOnMarkerComponent> ent, ref ApplyMarkerBonusEvent args)
    {
        var damage = ent.Comp.Boost;
        if (ent.Comp.BackstabBoost is {} backstab &&
            _backstab.TryBackstab(args.Target, args.User, Angle.FromDegrees(45d), playSound: false))
        {
            damage += backstab;
        }
        damage *= _pressure.GetModifier(ent.Owner);

        _damageable.ChangeDamage(args.Target, damage, origin: args.User);
    }
}
