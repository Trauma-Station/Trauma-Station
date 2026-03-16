using Content.Shared.Dataset;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitcode.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HereticKnowledgeRitualComponent : Component
{
    [DataField]
    public ProtoId<DatasetPrototype> KnowledgeDataset = "EligibleTags";

    [DataField]
    public float TagAmount = 4;

    /// <summary>
    /// Required tags for ritual of knowledge
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public HashSet<ProtoId<TagPrototype>> KnowledgeRequiredTags = new();
}
