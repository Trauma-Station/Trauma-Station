// SPDX-License-Identifier: AGPL-3.0-or-later

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
/// Gets all ConstructionSkills of a character.
/// </summary>
[ByRefEvent]
public record struct ConstructionGetGroupsEvent(Dictionary<EntProtoId, int> Groups);

/// <summary>
/// Called in order to add experience to a knowledge holder. Simply pass in a EntProtoId of the knowledge and the amount of exp you want to add.
/// User will default to the knowledge holder.
/// Pass user if e.g. you punch someone and the target gains a level, you are the user. The target can't predict any popups from it.
/// </summary>
[ByRefEvent]
public record struct AddExperienceEvent(EntProtoId KnowledgeType, int Experience, EntityUid? User = null);

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
/// Called in order to invoke sneak attack failure.
/// </summary>
[ByRefEvent]
public record struct InvokeSneakAttackSurprisedEvent();

/// <summary>
/// Called in order to invoke sneak attack failure.
/// </summary>
[ByRefEvent]
public record struct CanDoSneakAttackEvent(bool CanSneakAttack);

/// <summary>
/// Called in order to invoke damage modifiers for martial arts. Call on the art itself.
/// </summary>
[ByRefEvent]
public record struct MartialArtDamageModifierEvent(EntityUid User, float Coefficient = 1.0f);

/// <summary>
/// Called in order to invoke speed modifiers for martial arts. Call on the art itself.
/// </summary>
[ByRefEvent]
public record struct MartialArtSpeedModifierEvent(EntityUid User, float Coefficient = 1.0f);
