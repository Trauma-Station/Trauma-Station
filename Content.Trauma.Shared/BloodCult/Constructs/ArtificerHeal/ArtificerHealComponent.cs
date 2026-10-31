// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;

namespace Content.Trauma.Shared.BloodCult.Constructs.ArtificerHeal;

/// <summary>
/// Stores what damage types artificer should heal.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ArtificerHealComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier Damage;
}
