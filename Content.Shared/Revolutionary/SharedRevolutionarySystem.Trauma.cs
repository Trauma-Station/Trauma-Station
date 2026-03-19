using Content.Shared.Revolutionary.Components;

namespace Content.Shared.Revolutionary;

/// <Trauma>
/// Trauma - goob rev mindshield conversion changes
/// </Trauma>
public abstract partial class SharedRevolutionarySystem
{
    /// <summary>
    /// Change headrevs ability to convert people
    /// </summary>
    public void SetConvertAbility(Entity<HeadRevolutionaryComponent> headRev, bool enabled = true)
    {
        headRev.Comp.ConvertAbilityEnabled = enabled;
    }
}
