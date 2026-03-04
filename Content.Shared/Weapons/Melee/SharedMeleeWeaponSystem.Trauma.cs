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
using Content.Shared.Weapons.Ranged.Systems;
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
    [Dependency] private readonly SharedGunSystem _gun = default!;

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
        if (_knowledge.TryGetKnowledgeUnit(user, MeleeKnowledge) is { } melee && _knowledge.GetMastery(melee) > 2)
        {
            ev.Multipliers *= 1 + 2 * _knowledge.SharpCurve(melee, -50, 50.0f);
        }
    }

    private bool LightAttackMiss(EntityUid user, EntityUid target)
    {
        var knowledgeMiss = 1.0f;
        if (_knowledge.TryGetKnowledgeUnit(user, MeleeKnowledge) is { } melee)
        {
            if (_knowledge.GetMastery(melee) < 2)
            {
                knowledgeMiss = ((float) melee.Comp.Level + 5) / 26.0f;
            }
        }
        return _gun.Random(target).Prob(Math.Max(1.0f - knowledgeMiss, 0));
    }

    private void AddExperienceLight(EntityUid target)
    {
        if (MobState.IsAlive(target))
        {
            var evKnowledge = new AddExperienceEvent(MeleeKnowledge, 1);
            RaiseLocalEvent(target, ref evKnowledge);
        }
    }

    private void HeavyAttackMiss(EntityUid user, out Entity<KnowledgeComponent>? melee, ref List<EntityUid> entities)
    {
        melee = null;
        var knowledgeMiss = 1.0f;
        if (_knowledge.TryGetKnowledgeUnit(user, MeleeKnowledge) is { } meleeUnit)
        {
            melee = meleeUnit;
            if (_knowledge.GetMastery(meleeUnit) < 2)
            {
                knowledgeMiss = ((float) meleeUnit.Comp.Level + 2) / 26.0f;
            }
        }

        if (_gun.Random(user).Prob(Math.Max(1.0f - knowledgeMiss, 0)))
        {
            entities.Clear();
            entities.Add(user);
        }
    }

    private void AdjustStaminaDamage(Entity<KnowledgeComponent>? melee, ref float staminaDamage)
    {
        if (melee is { } meleeEnt)
        {
            staminaDamage *= 1 - _knowledge.SharpCurve(meleeEnt);
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

    private bool DisarmMiss(EntityUid user, out Entity<KnowledgeComponent>? melee)
    {
        melee = null;
        var knowledgeMiss = 1.0f;
        if (_knowledge.TryGetKnowledgeUnit(user, MeleeKnowledge) is { } meleeUnit)
        {
            melee = meleeUnit;
            if (_knowledge.GetMastery(meleeUnit) < 2)
            {
                knowledgeMiss = ((float) meleeUnit.Comp.Level + 10) / 26.0f;
            }
        }
        if (knowledgeMiss < 1.0f && _gun.Random(user).Prob(Math.Max(1.0f - knowledgeMiss, 0)))
            return true;
        return false;
    }

    private void DisarmExperience(Entity<KnowledgeComponent>? melee, EntityUid user, EntityUid target)
    {
        if (melee is { } meleeEnt && _knowledge.GetMastery(meleeEnt) < 2 && MobState.IsAlive(target))
        {
            var evKnowledge = new AddExperienceEvent(MeleeKnowledge, 1);
            RaiseLocalEvent(user, ref evKnowledge);
        }
    }
}
