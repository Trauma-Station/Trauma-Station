namespace Content.Trauma.Client.Overlays;

/// <summary>
/// Caches shaders for reusing in overlays
/// <seealso cref="ShaderCacheSystem"/>
/// </summary>
[RegisterComponent]
public sealed partial class ShaderCacheComponent : Component
{
    [ViewVariables]
    public Dictionary<string, ShaderInstance> Cache = new();
}
