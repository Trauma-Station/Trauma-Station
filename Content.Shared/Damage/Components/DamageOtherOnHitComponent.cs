// <Trauma>
using Robust.Shared.GameStates;
// </Trauma>
using Content.Shared.Damage.Systems;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Makes this entity deal damage when thrown at something.
/// </summary>
[RegisterComponent]
// Trauma - Deleted Access(typeof(SharedDamageOtherOnHitSystem))
[NetworkedComponent, AutoGenerateComponentState] // Trauma
public sealed partial class DamageOtherOnHitComponent : Component
{
    /// <summary>
    /// Whether to ignore damage modifiers.
    /// </summary>
    [DataField]
    public bool IgnoreResistances = false;

    /// <summary>
    /// The damage amount to deal on hit.
    /// </summary>
    [DataField(required: true)]
    [AutoNetworkedField] // Trauma
    public DamageSpecifier Damage = default!;

}
