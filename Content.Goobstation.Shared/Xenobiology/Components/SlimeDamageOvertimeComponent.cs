// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;

namespace Content.Goobstation.Shared.Xenobiology.Components;

/// <summary>
/// This is used for slime latching damage, this can be expanded in the future to allow for special breed dependent effects.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeDamageOvertimeComponent : Component
{
    [DataField]
    public EntityUid? SourceEntityUid;

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Caustic", 2.5 }
        }
    };
}
