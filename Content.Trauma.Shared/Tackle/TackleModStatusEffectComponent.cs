// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Tackle;

/// <summary>
/// Status effect component that modifies tackle effectiveness.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class TackleModStatusEffectComponent : Component
{
    /// <summary>
    /// Value to add to the tackle modifier event's Modifier field.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float Modifier;
}
