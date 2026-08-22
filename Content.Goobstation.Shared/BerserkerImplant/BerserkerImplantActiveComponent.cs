// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Goobstation.Shared.BerserkerImplant;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class BerserkerImplantActiveComponent : Component
{
    [DataField]
    public float Duration = 8;

    [DataField]
    public DamageModifierSet DamageModifier = new()
    {
        Coefficients = new()
        {
            { "Slash", 0.4f },
            { "Piercing", 0.4f },
            { "Ballistic", 0.4f },
            { "Blunt", 0.4f },
            { "Heat", 0.4f },
            { "Shock", 0.4f },
        }
    };

    [DataField]
    public float StunModifier = 0.5f;

    [DataField]
    public float SelfDamageModifier = 1.5f;

    [DataField]
    public DamageSpecifier DelayedDamage = new();

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan EndTime = TimeSpan.Zero;
}
