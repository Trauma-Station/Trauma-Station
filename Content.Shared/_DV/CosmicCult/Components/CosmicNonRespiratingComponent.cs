using Robust.Shared.GameStates;

namespace Content.Shared._DV.CosmicCult.Components;

/// <summary>
/// Makes the entity not suffocate in vacuum.
/// </summary>
[AutoGenerateComponentState]
[NetworkedComponent, RegisterComponent]
public sealed partial class CosmicNonRespiratingComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}
