using Robust.Shared.GameStates;

namespace Content.Shitcode.Shared.Heretic.Curses;

[RegisterComponent, NetworkedComponent]
public sealed partial class CurseOfParalysisStatusEffectComponent : Component
{
    [DataField]
    public bool WasParalyzed;
}
