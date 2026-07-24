// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.CCVar;
using Content.Goobstation.Common.Weapons;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Interaction.Components;
using Content.Shared.Item;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Content.Trauma.Common.Contests;
using Content.Trauma.Common.Knowledge.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Melee;

/// <summary>
/// Trauma - extra stuff for melee system
/// </summary>
public abstract partial class SharedMeleeWeaponSystem
{
    [Dependency] private CommonContestsSystem _contests = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private CommonKnowledgeSystem _knowledge = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

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

    private void AdjustStaminaDamage(EntityUid user, ref float staminaDamage)
    {
        // TODO: use event for this bruh
        if (_knowledge.GetKnowledge(user, MeleeKnowledge) is {} melee)
        {
            staminaDamage *= 1 - _knowledge.SharpCurve(melee);
        }
    }

    private void AddExtraDamageExamine(MeleeWeaponComponent component, DamageSpecifier damageSpec, FormattedMessage message)
    {
        var ap = component.ResistanceBypass ? 100 : (int) Math.Round(damageSpec.ArmorPenetration * 100);
        var clickMult = (int) Math.Round((component.ClickPartDamageMultiplier * component.ClickDamageModifier.Float() - 1f) * 100);
        var heavyMult = (int) Math.Round((component.HeavyPartDamageMultiplier - 1f) * 100);

        ModifyMessage(message, "armor-penetration", ap);
        ModifyMessage(message, "click-damage-modifier", clickMult);
        ModifyMessage(message, "heavy-damage-modifier", heavyMult);
    }

    private void ModifyMessage(FormattedMessage message, LocId loc, int value)
    {
        if (value == 0)
            return;
        var abs = Math.Abs(value);
        message.AddMarkupPermissive("\n" + Loc.GetString(loc, ("arg", value / abs), ("abs", abs)));
    }

    protected bool RaiseInRangeEvent(EntityUid ent,
        EntityUid target,
        float range,
        EntityCoordinates? targetCoordinates,
        Angle? targetAngle,
        out bool inRange,
        out EntityUid source)
    {
        var ev = new MeleeInRangeEvent(ent, target, range, targetCoordinates, targetAngle);
        RaiseLocalEvent(ent, ref ev);
        inRange = ev.InRange;
        source = ev.User;
        return ev.Handled;
    }
}
