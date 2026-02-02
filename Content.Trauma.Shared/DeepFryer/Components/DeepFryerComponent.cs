using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.DeepFryer.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DeepFryerComponent : Component
{
    [DataField]
    public TimeSpan TimeToCookMob = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan TimeToCookObject = TimeSpan.FromSeconds(10);

    [DataField]
    public float MinimumTempToStartCook = 500f;

    [DataField]
    public string FryerSolution = "fryer";

    [DataField]
    public ComponentRegistry ComponentsToRemove = new();

    [DataField]
    public ComponentRegistry ComponentsToAdd = new();

    [DataField]
    public SoundPathSpecifier StartSound = new("/Audio/_Trauma/Machines/DeepFryer/deep_fryer_initial.ogg");

    [DataField]
    public SoundPathSpecifier FinishSound = new("/Audio/_Trauma/Machines/DeepFryer/deep_fryer_done.ogg");
}

[Serializable, NetSerializable]
public enum DeepFryerVisuals : byte
{
    Open,
    Frying,
    BigFrying
}
