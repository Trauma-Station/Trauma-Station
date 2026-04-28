// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Damage.Components;

/// <summary>
/// Trauma - add IncreaseOnly field, works the same as in ProjectileComponent.
/// </summary>
public sealed partial class DamageOtherOnHitComponent
{
    [DataField]
    public bool IncreaseOnly;
}
