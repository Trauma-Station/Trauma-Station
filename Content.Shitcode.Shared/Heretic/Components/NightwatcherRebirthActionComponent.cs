namespace Content.Shared._Shitcode.Heretic.Components;

[RegisterComponent]
public sealed partial class NightwatcherRebirthActionComponent : Component
{
    [DataField]
    public TimeSpan CooldownReductionPerVictim = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan MinCooldown = TimeSpan.FromSeconds(10);

    [DataField]
    public int LastTargets;
}
