// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.StatusEffects;

/// <summary>
/// Status effect component that adds components to the target entity while active.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GrantComponentsStatusEffectComponent : Component
{
    [DataField(required: true)]
    [AlwaysPushInheritance]
    public ComponentRegistry Components = default!;
}
