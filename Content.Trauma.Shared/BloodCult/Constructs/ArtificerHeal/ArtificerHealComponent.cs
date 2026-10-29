// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;

namespace Content.Trauma.Shared.BloodCult.Constructs.ArtificerHeal;

/// <summary>
/// Marker component for the cult rule to track its constructs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ArtificerHealComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier Damage;
}
