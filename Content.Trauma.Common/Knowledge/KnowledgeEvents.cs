using Robust.Shared.Serialization;

namespace Content.Trauma.Common.Knowledge;

/// <summary>
/// Raised on a knowledge unit entity when it's added to some container entity.
/// </summary>
[ByRefEvent]
public record struct KnowledgeUnitAddedEvent(EntityUid Target);

/// <summary>
/// Raised on a knowledge unit entity when it's removed from some container entity.
/// </summary>
[ByRefEvent]
public record struct KnowledgeUnitRemovedEvent(EntityUid Target);

/// <summary>
/// Raised on all children of some entity to try to find an entity with <see cref="KnowledgeContainerComponent"/>
/// </summary>
[ByRefEvent] // Im not sure if it's the right way to do a relay, but whatever, it works.
public record struct KnowledgeContainerRelayEvent(EntityUid Target, EntityUid? Found = null, bool Handled = false);

/// <summary>
/// Event that is raised to get a description of some knowledge to display it in the character menu.
/// </summary>
[ByRefEvent]
public record struct KnowledgeGetDescriptionEvent(string? Description, bool Handled = false);

/// <summary>
/// Event that sends the client's wanted martial art entity to the server to update the martial art skill of the knowledge container component.  
/// </summary>
/// <param name="knowledge"></param>
[Serializable, NetSerializable]
public sealed class KnowledgeUpdateMartialArts(NetEntity? knowledge) : EntityEventArgs
{
    public NetEntity? Knowledge = knowledge;
}
