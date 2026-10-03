namespace Content.Shared.Mobs;

public sealed partial class DeathgaspComponent : Component
{
    /// <summary>
    /// Makes sure that the deathgasp is only displayed if the entity went critical before dying
    /// </summary>
    [DataField]
    public bool NeedsCritical = true;
}
