using System.Diagnostics.Metrics;
using System.Linq;
using Content.Goobstation.Common.CCVar;
using Content.Shared._EinsteinEngines.Contests;
using Content.Shared.Coordinates;
using Content.Shared.Damage.Events;
using Content.Shared.Interaction.Components;
using Content.Shared.Item;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

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
    [Dependency] private readonly CommonKnowledgeSystem _knowledge = default!;

    private EntityQuery<InteractionRelayComponent> _relayQuery;

    public static readonly ProtoId<TagPrototype> WideSwingIgnore = "WideSwingIgnore"; // for mice
    public static readonly EntProtoId MeleeKnowledge = "MeleeKnowledge";

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

    private void AdjustAttackRate(EntityUid user, ref GetMeleeAttackRateEvent ev)
    {
        if (_knowledge.GetKnowledge(user, MeleeKnowledge) is {} melee && _knowledge.GetMastery(melee.Comp) > 2)
        {
            // FIXME: this is too fast? also why is it here for fuck sake
            ev.Multipliers *= 1 + 2 * _knowledge.SharpCurve(melee, -50, 50.0f);
        }
    }

    private void AddExperienceLight(EntityUid user)
    {
        if (!MobState.IsAlive(user))
            return;

        var evKnowledge = new AddExperienceEvent(MeleeKnowledge, 1);
        RaiseLocalEvent(user, ref evKnowledge);
    }

    private void AdjustStaminaDamage(EntityUid user, ref float staminaDamage)
    {
        // TODO: use event for this bruh
        if (_knowledge.GetKnowledge(user, MeleeKnowledge) is {} melee)
        {
            staminaDamage *= 1 - _knowledge.SharpCurve(melee);
        }
    }

    private void AddExperienceHeavy(EntityUid user, ref List<EntityUid> entities)
    {
        if (entities.Count > 0 && entities.Any(entity => MobState.IsAlive(entity)))
        {
            var evKnowledge = new AddExperienceEvent(MeleeKnowledge, entities.Count(entity => MobState.IsAlive(entity)));
            RaiseLocalEvent(user, ref evKnowledge);
        }
    }

    private void DisarmExperience(EntityUid user, EntityUid target)
    {
        // TODO: move all this shit to event handlers bruh
        if (_knowledge.GetKnowledge(user, MeleeKnowledge) is {} melee && _knowledge.GetMastery(melee.Comp) < 2 && MobState.IsAlive(target))
        {
            var evKnowledge = new AddExperienceEvent(MeleeKnowledge, 1);
            RaiseLocalEvent(user, ref evKnowledge);
        }
    }
}
