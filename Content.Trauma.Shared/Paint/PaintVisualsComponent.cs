using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Paint;

/// <summary>
/// Component for changing non-shaded layers of an entity to have a greyscale shader and specific color.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(PaintSystem))]
[AutoGenerateComponentState(true)]
public sealed partial class PaintVisualsComponent : Component
{
    /// <summary>
    /// The color of paint used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color Color = Color.White;

    /// <summary>
    /// Every changed layer index and the color it previously had.
    /// </summary>
    [ViewVariables]
    public Dictionary<int, Color> LayerColors = new();
}

/// <summary>
/// Event raised on the painted entity if it gets painted/repainted.
/// Used for predicting the sprite changes.
/// </summary>
[ByRefEvent]
public record struct PaintedEvent();
