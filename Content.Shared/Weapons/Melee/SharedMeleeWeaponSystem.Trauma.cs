using System.Linq;
using Content.Goobstation.Common.CCVar;
using Content.Shared._EinsteinEngines.Contests;
using Content.Shared.Coordinates;
using Content.Shared.Interaction.Components;
using Content.Shared.Item;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Systems;
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
    [Dependency] private readonly SharedGunSystem _gun = default!;

    private EntityQuery<InteractionRelayComponent> _relayQuery;

    public static readonly ProtoId<TagPrototype> WideSwingIgnore = "WideSwingIgnore"; // for mice
    public static readonly EntProtoId MeleeKnowledge = "MeleeKnowledge";
    public static readonly EntProtoId WeaponsKnowledge = "WeaponsKnowledge";

    private float _shoveRange;
    private float _shoveSpeed;
    private float _shoveMass;

    private void InitializeTrauma()
    {
        _relayQuery = GetEntityQuery<InteractionRelayComponent>();

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

    private bool LightAttackMiss(EntityUid user)
    {
        var ev = new MissAttackEvent(5);
        RaiseLocalEvent(user, ev);

        if (ev.Miss)
            PopupSystem.PopupClient(Loc.GetString("container-thrown-missed"), user, user);

        return ev.Miss;
    }

    private void AddExperienceLight(EntityUid target, EntityUid user)
    {
        if (MobState.IsAlive(target) && target != user)
        {
            var evKnowledge = new AddExperienceEvent(MeleeKnowledge, 1);
            RaiseLocalEvent(user, ref evKnowledge);
        }
    }

    private void HeavyAttackMiss(EntityUid user, ref List<EntityUid> entities)
    {
        var ev = new MissAttackEvent(2);
        RaiseLocalEvent(user, ev);

        if (ev.Miss)
        {
            entities.Clear();
            entities.Add(user);
        }
    }

    private void AddExperienceHeavy(EntityUid user, ref List<EntityUid> entities)
    {
        if (entities.Count(entity => MobState.IsAlive(entity)) is var count and > 0)
        {
            var evKnowledge = new AddExperienceEvent(MeleeKnowledge, count);
            RaiseLocalEvent(user, ref evKnowledge);
            var evWeapons = new AddExperienceEvent(WeaponsKnowledge, 1);
            RaiseLocalEvent(user, ref evWeapons);
        }
    }

    private bool DisarmMiss(EntityUid user)
    {
        var ev = new MissAttackEvent(10);
        RaiseLocalEvent(user, ev);

        return ev.Miss;
    }
}
