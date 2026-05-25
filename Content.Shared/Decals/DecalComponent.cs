using Robust.Shared.GameStates;

namespace Content.Shared.Decals;

/// <summary>
/// Trauma - decals rewrite, component for all decal entities. Gets decal data loaded from the map etc.
/// Decal entities themselves are not saved as to not massively bloat map yml, it still uses the old DecalGrid saving.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedDecalSystem))]
[AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class DecalComponent : Component
{
    /// <summary>
    /// The decal data used for drawing.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Decal Data = default!;

    /// <summary>
    /// Cached tile coordinates on the decal's grid.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Vector2i Chunk;
}
