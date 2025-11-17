using Robust.Shared.GameStates;

namespace Content.Goobstation.Common.Traits.Components;

// whoever put this in common fuck you
[RegisterComponent, NetworkedComponent]
public sealed partial class SocialAnxietyComponent : Component
{
    [DataField] public TimeSpan DownedTime = TimeSpan.FromSeconds(3);
}
