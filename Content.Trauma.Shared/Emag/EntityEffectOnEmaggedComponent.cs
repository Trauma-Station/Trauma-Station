// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Emag;

[RegisterComponent, NetworkedComponent]
public sealed partial class EntityEffectOnEmaggedComponent : Component
{
    /// <summary>
    /// The effects to apply.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects;

    /// <summary>
    /// Optional scale multiplier for the effects.
    /// </summary>
    [DataField]
    public float Scale = 1f;
}
