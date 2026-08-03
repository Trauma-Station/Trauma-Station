// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Body.Organ;

/// <summary>
/// Component for organs that adds permanent status effects to the body while the organ is enabled.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(OrganStatusEffectsSystem))]
public sealed partial class OrganStatusEffectsComponent : Component
{
    [DataField(required: true)]
    public List<EntProtoId> StatusEffects = default!;
}
