// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Tackle;

/// <summary>
/// Added to special equipment or mobs to allow tackles
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TackleModifierComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<TackleModifier> Modifiers = new();
}

[Serializable, NetSerializable, DataRecord]
public sealed partial class TackleModifier : IComparable<TackleModifier>
{
    /// <summary>
    /// Whether values of this will affect other tackle modifiers
    /// </summary>
    public bool AffectsOtherModifiers;

    /// <summary>
    /// Priority of this modifier when selecting tackle source entity
    /// Null if it cannot be used for tackling
    /// </summary>
    public int? Priority;

    /// <summary>
    /// Multiplier to tackle throw speed
    /// </summary>
    public float SpeedMultiplier = 1f;

    /// <summary>
    /// Multiplier to tackle throw range
    /// </summary>
    public float RangeMultiplier = 1f;

    /// <summary>
    /// Multiplier to tackle cooldown
    /// </summary>
    public float CooldownMultiplier = 1f;

    /// <summary>
    /// Multiplier to knockdown time when performing tackle
    /// </summary>
    public float KnockdownTimeMultiplier = 1f;

    /// <summary>
    /// Multiplier to stamina cost of tackle
    /// </summary>
    public float StaminaCostMultiplier = 1f;

    /// <summary>
    /// The higher this is, the more velocity is relevant when calculating modifiers during tackle collision
    /// </summary>
    public float SpeedModMultiplier = 1f;

    /// <summary>
    /// Minimal "safe" distance, if tackle collision happens below safe range, user will be hurt
    /// </summary>
    public float MinDistance;

    /// <summary>
    /// How relevant is stamina damage resistance on target. Higher = more relevant
    /// </summary>
    public float StamResistModifier = 1f;

    /// <summary>
    /// If result modifier exceeds this value, target will be disarmed on knockdown
    /// </summary>
    public float DisarmThreshold = 1f;

    /// <summary>
    /// Bonus modifier to user tackle
    /// </summary>
    public float SkillMod;

    /// <summary>
    /// If true, user will grab target on successful tackle outcome
    /// </summary>
    public bool GrabOnSuccess;

    /// <summary>
    /// Modifier to how much damage/paralyze time will the user suffer from when hitting a wall
    /// </summary
    public float SeverityModifier = 1f;

    /// <summary>
    /// Multiplies user damage upon hitting the wall by this
    /// </summary
    public float UserDamageMultiplier = 1f;

    /// <summary>
    /// Multiplies knockdown time for user on collision
    /// </summary
    public float UserKnockdownTimeMultiplier = 1f;

    /// <summary>
    /// Multiplies target stamina damage on collide
    /// </summary
    public float TargetStaminaDamageMultiplier = 1f;

    /// <summary>
    /// Multiplies target knockdown time on collide
    /// </summary
    public float TargetKnockdownTimeMultiplier = 1f;

    /// <summary>
    /// Will this even collide and cause knockdown/stamina/damage on user or target?
    /// </summary>
    public bool AllowCollision = true;

    /// <summary>
    /// If this is non null and fails, this modifier will be ignored
    /// </summary>
    public ProtoId<EntityConditionPrototype>? TackleCondition;

    /// <summary>
    /// Effects applied to user when tackling
    /// Only applies when this tackle modifier is tackle "source"
    /// </summary>
    public ProtoId<EntityEffectPrototype>? UserEffect;

    public void Modify(TackleModifier other)
    {
        if (!AffectsOtherModifiers)
            return;

        other.SpeedMultiplier *= SpeedMultiplier;
        other.RangeMultiplier *= RangeMultiplier;
        other.CooldownMultiplier *= CooldownMultiplier;
        other.KnockdownTimeMultiplier *= KnockdownTimeMultiplier;
        other.StaminaCostMultiplier *= StaminaCostMultiplier;
        other.SpeedModMultiplier *= SpeedModMultiplier;
        other.MinDistance = Math.Max(other.MinDistance, MinDistance);
        other.SkillMod += SkillMod;
        other.GrabOnSuccess |= GrabOnSuccess;
        other.SeverityModifier *= SeverityModifier;
        other.UserDamageMultiplier *= UserDamageMultiplier;
        other.UserKnockdownTimeMultiplier *= UserKnockdownTimeMultiplier;
        other.TargetStaminaDamageMultiplier *= TargetStaminaDamageMultiplier;
        other.TargetKnockdownTimeMultiplier *= TargetKnockdownTimeMultiplier;
        other.AllowCollision |= AllowCollision;
    }

    public int CompareTo(TackleModifier? other)
    {
        if (other is not { } mod || mod.Priority is not { } otherPriority)
            return 1;

        if (Priority is not { } ourPriority)
            return -1;

        return ourPriority.CompareTo(otherPriority);
    }
}
