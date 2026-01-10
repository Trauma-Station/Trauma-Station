using Robust.Shared.GameStates;

namespace Content.Trauma.Common.MartialArts;

[RegisterComponent]
public sealed partial class MartialArtBlockedComponent : Component;

[RegisterComponent]
[NetworkedComponent]
public abstract partial class GrabStagesOverrideComponent : Component
{
    [DataField]
    public GrabStage StartingStage = GrabStage.Soft;
}

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class MartialArtsKnowledgeComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public bool Blocked;
}
