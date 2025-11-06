using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Genetics.Abilities;

/// <summary>
/// Mutation that modifies bleeding rate and blood refresh rate.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(BleedingMutationSystem))]
public sealed partial class BleedingMutationComponent : Component
{
    [DataField]
    public float BleedModifier = 1.5f;

    [DataField]
    public float RefreshModifier = 6f;
}
