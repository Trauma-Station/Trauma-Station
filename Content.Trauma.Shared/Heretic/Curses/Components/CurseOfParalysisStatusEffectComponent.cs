using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Heretic.Curses.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CurseOfParalysisStatusEffectComponent : Component
{
    [DataField]
    public bool WasParalyzed;
}
