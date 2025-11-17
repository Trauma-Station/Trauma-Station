using Content.Shared.Body.Systems;
using Robust.Shared.GameStates; // Trauma

namespace Content.Shared.Body.Components;

[RegisterComponent, Access(typeof(BrainSystem))]
[NetworkedComponent, AutoGenerateComponentState] // Trauma
public sealed partial class BrainComponent : Component
{
    /// <summary>
    /// Shitmed Change: Is this brain currently controlling the entity?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active = true;
}
