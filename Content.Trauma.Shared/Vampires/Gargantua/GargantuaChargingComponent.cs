// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Vampires.Gargantua;

/// <summary>
/// Component applied to a Gargantua Vampire during a charge action.
/// Applies entity effects on collision with other entities, and gets removed during a landing event.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GargantuaChargingComponent : Component
{
    /// <summary>
    /// Effects that run when the entity collides with other entities.
    /// </summary>
    [DataField]
    public EntityEffect[] ImpactEffects = default!;
}
