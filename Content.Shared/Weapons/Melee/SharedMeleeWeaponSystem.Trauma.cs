using Content.Goobstation.Common.CCVar;
using Content.Shared._EinsteinEngines.Contests;
using Content.Shared.Coordinates;
using Content.Shared.Damage.Events;
using Content.Shared.Item;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Melee;

/// <summary>
/// Trauma - extra stuff for melee system
/// </summary>
public abstract partial class SharedMeleeWeaponSystem
{
    [Dependency] private readonly ContestsSystem _contests = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public static readonly ProtoId<TagPrototype> WideSwingIgnore = "WideSwingIgnore"; // for mice

    private float _shoveRange;
    private float _shoveSpeed;
    private float _shoveMass;

    private void InitializeTrauma()
    {
        Subs.CVar(_cfg, GoobCVars.ShoveRange, x => _shoveRange = x, true);
        Subs.CVar(_cfg, GoobCVars.ShoveSpeed, x => _shoveSpeed = x, true);
        Subs.CVar(_cfg, GoobCVars.ShoveMassFactor, x => _shoveMass = x, true);
    }

    public bool AttemptHeavyAttack(EntityUid user, EntityUid weaponUid, MeleeWeaponComponent weapon, List<EntityUid> targets, EntityCoordinates coordinates)
        => AttemptAttack(user,
            weaponUid,
            weapon,
            new HeavyAttackEvent(GetNetEntity(weaponUid), GetNetEntityList(targets), GetNetCoordinates(coordinates)),
            null);

    private float CalculateShoveStaminaDamage(EntityUid disarmer, EntityUid disarmed)
    {
        var baseStaminaDamage = TryComp<ShovingComponent>(disarmer, out var shoving) ? shoving.StaminaDamage : ShovingComponent.DefaultStaminaDamage;

        return baseStaminaDamage * _contests.MassContest(disarmer, disarmed);
    }

    private void PhysicalShove(EntityUid user, EntityUid target)
    {
        var force = _shoveRange * _contests.MassContest(user, target, rangeFactor: _shoveMass);

        var userPos = TransformSystem.ToMapCoordinates(user.ToCoordinates()).Position;
        var targetPos = TransformSystem.ToMapCoordinates(target.ToCoordinates()).Position;
        if (userPos == targetPos)
            return; // no NaN

        var pushVector = (targetPos - userPos).Normalized() * force;

        var animated = HasComp<ItemComponent>(target);

        _throwing.TryThrow(target, pushVector, force * _shoveSpeed, animated: animated);
    }
}
