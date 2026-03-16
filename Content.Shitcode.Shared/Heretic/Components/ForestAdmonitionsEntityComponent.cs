using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Shitcode.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ForestAdmonitionsEntityComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastRevealTime;

    [DataField]
    public float RevealDuration = 5f;

    [DataField]
    public float RevealDistance = 2f;

    [DataField]
    public float SelfVisibility = 0.2f;

    [DataField]
    public float ExamineThreshold = 0.2f;

    [DataField]
    public float UpdateTime = 0.1f;

    [ViewVariables]
    public float UpdateAccumulator;
}
