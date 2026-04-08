// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Knowledge.Attribute.Attribute.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class StrengthFeatTierdownComponent : Component
{
    [DataField]
    public float Mod;
}
