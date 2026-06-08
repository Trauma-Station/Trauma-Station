using Content.Shared.EntityEffects;

namespace Content.Shared.Tiles; // Trauma - moved to shared

/// <summary>
/// Applies effects upon stepping onto a tile.
/// </summary>
[RegisterComponent, Access(typeof(TileEntityEffectSystem))]
public sealed partial class TileEntityEffectComponent : Component
{
    /// <summary>
    /// List of effects that should be applied.
    /// </summary>
    [DataField(required: true)] // Trauma - required, makes no sense for this to be empty
    public List<EntityEffect> Effects = default!;
}
