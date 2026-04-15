namespace Content.Trauma.Shared.Airlocks;

[RegisterComponent, NetworkedComponent]
public sealed partial class AirlockStripesComponent : Component
{
    [DataField(required: true)]
    public Color Color;

    [DataField]
    public string OpeningSpriteState = "open_stripe";

    [DataField]
    public string ClosingSpriteState = "closing_stripe";

    [DataField]
    public string ClosedSpriteState = "closed_stripe";

    public const string AnimationKey = "door_stripes_animation";

    public object OpeningAnimation = default!;

    public object ClosingAnimation = default!;
}

public enum AirlockStripesLayers : byte
{
    Stripes
}
