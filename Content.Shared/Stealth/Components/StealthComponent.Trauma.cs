namespace Content.Shared.Stealth.Components;

public sealed partial class StealthComponent : Component
{
    /// <summary>
    /// The creature will continue invisible at Crit.
    /// </summary>
    [DataField]
    public bool EnabledOnCrit = true;

    /// <summary>
    /// Remove stealth if an attack is made
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RevealOnAttack = true;

    /// <summary>
    /// Remove stealth if an attack is made
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RevealOnDamage = true;

    /// <summary>
    /// Adds a threshold for when taking damage so you dont get revealed by taking airloss or bleeding etc.
    /// </summary>
    [DataField]
    public float Threshold = 5;

    /// <summary>
    /// Is this entity hidden from thermal vision while stealthed?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ThermalsImmune;
}
