using System.Numerics;

namespace Content.Client._DV.CosmicCult;

[RegisterComponent]
public sealed partial class CosmicMarkVisualsComponent : Component
{
    /// <summary>
    /// An offset applied to the star mark (and subtle mark). Only checked when the star mark is added.
    /// </summary>
    [DataField]
    public Vector2 Offset = Vector2.Zero;

    /// <summary>
    /// RSI state that will be used by the subtle mark. Only checked when the subtle mark is added.
    /// </summary>
    [DataField]
    public string SubtleState = "default";

    /// <summary>
    /// RSI state that will be used by the star mark. Only checked when the star mark is added.
    /// </summary>
    [DataField]
    public string StarState = "default";
}
