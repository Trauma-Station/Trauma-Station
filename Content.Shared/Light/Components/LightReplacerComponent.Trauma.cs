using Robust.Shared.Prototypes;

namespace Content.Shared.Light.Components;

public sealed partial class LightReplacerComponent
{
    /// <summary>
    /// How much glass is inside of the light replacer.
    /// <see cref="GlassRequired"/> means it will create a new bulb.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int GlassRecycled;

    /// <summary>
    /// How much glass required for one bulb.
    /// </summary>
    [DataField]
    public int GlassRequired = 4;

    /// <summary>
    /// How much glass given per bulb recycled.
    /// </summary>
    [DataField]
    public int GlassPerBulb = 1;

    /// <summary>
    /// What bulb is spawned when the max glass is reached?
    /// </summary>
    [DataField]
    public EntProtoId LightBulbProto = "LedLightTube";
}
