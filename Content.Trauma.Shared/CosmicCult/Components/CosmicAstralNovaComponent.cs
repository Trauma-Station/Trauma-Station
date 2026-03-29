// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.CosmicCult.Components;

/// <summary>
/// Component to call back to the cosmic cult ability system regarding a collision
/// </summary>
[NetworkedComponent, RegisterComponent]
public sealed partial class CosmicAstralNovaComponent : Component
{
    [DataField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(4f);

    [DataField]
    public DamageSpecifier AreaDamage = new()
    {
        DamageDict = new() {
            { "Asphyxiation", 5 }
        }
    };

    [DataField]
    public float AreaRange = 2.5f;

    /// <summary>
    ///     Entities not affected by astral nova
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist AreaBlacklist = new();
}
