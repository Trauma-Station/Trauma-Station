using Robust.Shared.GameStates;

namespace Content.Trauma.Common.MartialArts;

[RegisterComponent, NetworkedComponent]
public sealed partial class GrabStagesOverrideComponent : Component
{
    [DataField]
    public GrabStage StartingStage = GrabStage.Soft;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MartialArtsKnowledgeComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Blocked;

    [DataField, AutoNetworkedField]
    public int TemporaryBlockedCounter;
}
