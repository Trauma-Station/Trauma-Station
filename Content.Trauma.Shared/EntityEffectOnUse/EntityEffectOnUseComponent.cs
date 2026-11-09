// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffectOnUse;

/// <summary>
/// Applyes effects on use in hand.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EntityEffectOnUseComponent : Component
{
    /// <summary>
    /// Effects that will run on the item being used
    /// </summary>
    [DataField]
    public EntityEffect[]? Effects;

    /// <summary>
    /// Optionally, effects that will run on the user.
    /// These effects run before the other effects.
    /// </summary>
    [DataField]
    public EntityEffect[]? UserEffects;
}
