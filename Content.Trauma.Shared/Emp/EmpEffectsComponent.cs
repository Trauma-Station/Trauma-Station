// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Emp;

/// <summary>
/// Applies an effect prototype to this entity when it gets hit by an EMP.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(EmpEffectsSystem))]
[AutoGenerateComponentState]
public sealed partial class EmpEffectsComponent : Component
{
    /// <summary>
    /// Effect prototype to apply to this entity.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<EntityEffectPrototype> Effects;

    /// <summary>
    /// Whether to mark the entity as disabled by the EMP.
    /// </summary>
    [DataField]
    public bool Disable;
}
