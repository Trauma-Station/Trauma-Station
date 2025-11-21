using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.AudioMuffle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class SoundBlockerComponent : Component
{
    [DataField, AutoNetworkedField]
    public float SoundBlockPercent = 0.7f;

    [DataField, AutoNetworkedField]
    public bool Active = true;
}
