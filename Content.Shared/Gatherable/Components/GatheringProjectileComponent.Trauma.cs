namespace Content.Shared.Gatherable.Components;

public sealed partial class GatheringProjectileComponent
{
    /// Goobstation
    /// The probability that the given projectile will actually be gathering
    /// </summary>
    [DataField]
    public float Probability = 1f;
}
