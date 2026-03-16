using Robust.Shared.GameStates;

namespace Content.DV.Shared.CosmicCult.Components;

/// <summary>
/// Marks an entity that has been sacrificed to the monument.
/// </summary>
[AutoGenerateComponentState]
[NetworkedComponent, RegisterComponent]
public sealed partial class CosmicSacrificedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid AstralForm;

    [DataField, AutoNetworkedField]
    public bool WasNonRespirating;
}
