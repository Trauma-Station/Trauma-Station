// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Genetics.Abilities;

/// <summary>
/// Adds permanetn status effects while this mutation is active.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(StatusEffectsMutationSystem))]
public sealed partial class StatusEffectsMutationComponent : Component
{
    [DataField(required: true)]
    public List<EntProtoId> StatusEffects = default!;
}
