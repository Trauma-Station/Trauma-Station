namespace Content.Shared.EntityEffects;

/// <summary>
/// Trauma - add ScaleProbability
/// </summary>
public abstract partial class EntityEffect
{
    /// <summary>
    /// If true, probability will be increased/decreased by effect scale.
    /// </summary>
    [DataField]
    public bool ScaleProbability;
}
