// SPDX-License-Identifier: AGPL-3.0-or-later

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
