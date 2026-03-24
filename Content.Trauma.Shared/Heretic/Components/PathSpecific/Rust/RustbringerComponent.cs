// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Rust;

[RegisterComponent, NetworkedComponent]
public sealed partial class RustbringerComponent : Component
{
    [DataField]
    public DamageModifierSet ModifierSet = new()
    {
        Coefficients =
        {
            { "Caustic", 0f },
            { "Poison", 0f },
            { "Radiation", 0f },
            { "Cellular", 0f },
        },
    };

    [DataField]
    public EntProtoId Effect = "TileHereticRustRune";

    [ViewVariables(VVAccess.ReadWrite)]
    public float Accumulator;

    [DataField]
    public float Delay = 0.2f;
}
