using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid.Prototypes;

public sealed partial class RandomHumanoidSettingsPrototype
{
    /// <summary>
    /// Species that will be forced, instead of picking a random one.
    /// </summary>
    [DataField]
    public ProtoId<SpeciesPrototype>? SpeciesWhitelist;
}
