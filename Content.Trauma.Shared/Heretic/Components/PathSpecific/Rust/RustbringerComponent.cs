// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Rust;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
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

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromMilliseconds(200);
}
