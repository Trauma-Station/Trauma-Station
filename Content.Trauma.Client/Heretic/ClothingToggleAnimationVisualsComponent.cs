using Content.Trauma.Shared.Heretic.Components;

namespace Content.Trauma.Client.Heretic;

[RegisterComponent]
public sealed partial class ClothingToggleAnimationVisualsComponent : Component
{
    [DataField]
    public ToggleAnimationState State = ToggleAnimationState.All;
}
