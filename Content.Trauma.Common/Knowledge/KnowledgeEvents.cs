// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Trauma.Common.Knowledge;

/// <summary>
/// Event that sends the client's wanted martial art entity to the server to update the martial art skill of the knowledge container component.
/// </summary>
/// <param name="knowledge"></param>
[Serializable, NetSerializable]
public sealed class KnowledgeUpdateMartialArtsEvent(NetEntity? knowledge) : EntityEventArgs
{
    public NetEntity? Knowledge = knowledge;
}

/// <summary>
/// Event that is raised to get a description of some knowledge to display it in the character menu.
/// </summary>
[ByRefEvent]
public record struct KnowledgeCopyEvent(EntityUid? Target);

/// <summary>
/// Gets all ConstructionSkills of a character.
/// </summary>
/// <param name="Groups"></param>
[ByRefEvent]
public record struct ConstructionGetGroupsEvent(Dictionary<EntProtoId, int> Groups);

/// <summary>
/// Called in order to add experience to the character. Simply pass in a EntProtoId of the knowledge and the amount of exp you want to add.
/// </summary>
/// <param name="KnowledgeType"></param>
/// <param name="Experience"></param>
[ByRefEvent]
public record struct AddExperienceEvent(EntProtoId KnowledgeType, int Experience);

/// <summary>
/// Called in order to update the experience of the character, need be.
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
