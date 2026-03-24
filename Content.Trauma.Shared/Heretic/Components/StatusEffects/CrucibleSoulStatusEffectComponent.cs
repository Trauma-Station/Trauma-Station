using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Trauma.Shared.Heretic.Components.StatusEffects;

[RegisterComponent, NetworkedComponent]
public sealed partial class CrucibleSoulStatusEffectComponent : Component
{
    [DataField]
    public EntityCoordinates? Coords;
}
