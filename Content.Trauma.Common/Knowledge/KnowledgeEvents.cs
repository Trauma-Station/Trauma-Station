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
///
/// </summary>
[ByRefEvent]
public record struct MissAttackEvent(int Adjust, bool Miss = false);
