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

    /// <summary>
    /// Whether the entity needs to breathe when alive
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool EnableWhenAlive = true;

    /// <summary>
    /// Whether the entity needs to breathe when critical. This allows the entity to stay in crit forever, so it's usually off.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool EnableWhenCritical = false;
}
