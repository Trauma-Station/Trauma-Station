// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Damage;

/// <summary>
/// Deals damage to a mob on body init.
/// Does nothing to non mobs, use <c>Damageable.damage</c> instead
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(DefaultBodyDamageSystem))]
public sealed partial class DefaultBodyDamageComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier Damage = new();
}
