// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Trauma.Common.Knowledge;

/// <summary>
/// Event that sends the client's wanted martial art id to the server to update the active martial art skill.
/// </summary>
[Serializable, NetSerializable]
public sealed class KnowledgeUpdateMartialArtsEvent(EntProtoId? knowledge) : EntityEventArgs
{
    public readonly EntProtoId? Knowledge = knowledge;
}

/// <summary>
/// Raised to let the client update XP ui stuff.
/// </summary>
[ByRefEvent]
public record struct UpdateExperienceEvent();

/// <summary>
/// Called in order to invoke modifier to an item quality.
/// </summary>
[ByRefEvent]
public record struct UpdateItemQualityEvent(EntityUid User);

/// <summary>
/// Called in order to invoke damage modifiers for martial arts. Call on the art itself.
/// </summary>
[ByRefEvent]
public record struct MartialArtDamageModifierEvent(EntityUid User, float Coefficient = 1.0f);

/// <summary>
/// Raised on the attacker. Determines if attacker continues strike or no.
/// </summary>
[ByRefEvent]
public record struct ActiveMeleeResolveEvent(EntityUid Defender, EntityUid Weapon, DamageSpecifier Damage, bool Cancelled = false);

/// <summary>
/// Raised on the projectile. Determines if projectile strikes or not.
/// </summary>
[ByRefEvent]
public record struct ActiveProjectileResolveEvent(EntityUid Defender, EntityUid Weapon, bool Cancelled = false);

/// <summary>
/// Raised on the defender. Get Possible Defense Dice.
/// </summary>
[ByRefEvent]
public record struct GetDefenseDice(int Dice = 8);

/// <summary>
/// Raised on entity getting hit by a critical hit.
/// </summary>
[ByRefEvent]
public record struct CriticalHitEvent(EntityUid Attacker, DamageSpecifier Damage);

/// <summary>
/// Raised on entity whenever they fumble.
/// </summary>
[ByRefEvent]
public record struct OnFumbleEvent(int FumbleDifference);
