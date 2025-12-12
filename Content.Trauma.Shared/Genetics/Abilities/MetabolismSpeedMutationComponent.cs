using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Genetics.Abilities;

/// <summary>
/// Mutation component that increases mob's flat metabolism rate when active.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MetabolismSpeedMutationComponent : Component
{
    [DataField(required: true)]
    public float Bonus;
}
