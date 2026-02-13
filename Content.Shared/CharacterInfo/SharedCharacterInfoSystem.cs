using Content.Trauma.Common.Knowledge;
using Content.Shared.Objectives;
using Robust.Shared.Serialization;

namespace Content.Shared.CharacterInfo;

[Serializable, NetSerializable]
public sealed class RequestCharacterInfoEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;

    public RequestCharacterInfoEvent(NetEntity netEntity)
    {
        NetEntity = netEntity;
    }
}

[Serializable, NetSerializable]
public sealed class CharacterInfoEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;
    public readonly string JobTitle;
    public readonly Dictionary<string, List<ObjectiveInfo>> Objectives;
    public readonly string? Briefing;
    public readonly Dictionary<string, List<KnowledgeInfo>> Knowledge; // Trauma - Knowledge

    public CharacterInfoEvent(NetEntity netEntity, string jobTitle, Dictionary<string, List<ObjectiveInfo>> objectives, string? briefing, Dictionary<string, List<KnowledgeInfo>> knowledge)  // Goobstation edit - added knowledge
    {
        NetEntity = netEntity;
        JobTitle = jobTitle;
        Objectives = objectives;
        Briefing = briefing;
        Knowledge = knowledge;
    }
}
