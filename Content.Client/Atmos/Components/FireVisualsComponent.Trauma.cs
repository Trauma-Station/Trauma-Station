namespace Content.Client.Atmos.Components;

public sealed partial class FireVisualsComponent
{
    /// <summary>
    /// Hardlink for the holy fire effect to be used in tandem with the fire effect.
    /// </summary>
    [DataField]
    public string? SpriteHoly;

    /// <summary>
    /// Color for the holy fire light.
    /// </summary>
    [DataField]
    public Color LightColorHoly = Color.Blue;

    /// <summary>
    /// This is a light entity, same as the LightEntity variable above.
    /// </summary>
    public EntityUid? LightEntityHoly;
}
