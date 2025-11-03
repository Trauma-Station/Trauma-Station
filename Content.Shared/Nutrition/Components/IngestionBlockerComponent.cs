using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameStates; // Trauma

namespace Content.Shared.Nutrition.Components;

/// <summary>
///     Component that denotes a piece of clothing that blocks the mouth or otherwise prevents eating & drinking.
/// </summary>
/// <remarks>
///     In the event that more head-wear & mask functionality is added (like identity systems, or raising/lowering of
///     masks), then this component might become redundant.
/// </remarks>
[RegisterComponent, Access(typeof(IngestionSystem))]
[NetworkedComponent, AutoGenerateComponentState] // Trauma
public sealed partial class IngestionBlockerComponent : Component
{
    /// <summary>
    ///     Is this component currently blocking consumption.
    /// </summary>
    [DataField, AutoNetworkedField] // Trauma - networked
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Goob - Blocks smoke inhalation when this mask is down, even if internals are off.
    /// </summary>
    [DataField]
    public bool BlockSmokeIngestion;
}
